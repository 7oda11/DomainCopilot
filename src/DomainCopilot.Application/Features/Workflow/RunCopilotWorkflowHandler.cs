using System.Net.Http;
using System.Text.RegularExpressions;
using DomainCopilot.Application.Features.Agents;
using DomainCopilot.Application.Features.Retrieval;
using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Application.Abstractions.Observability;
using DomainCopilot.Application.Errors;
using DomainCopilot.Domain.Workflows;

namespace DomainCopilot.Application.Features.Workflow;

public sealed partial class RunCopilotWorkflowHandler(
    SearchGuidelinesTool searchGuidelines,
    ReadClinicalCaseTool readClinicalCase,
    ValidateCitationsTool validateCitations,
    QueueClinicalDraftTool queueClinicalDraft,
    GuidelineResearcherAgent researcher,
    DocumentationDrafterAgent drafter,
    SafetyCheckerAgent safetyChecker,
    IRepository<CopilotRun> runs,
    IWorkflowTraceStore traceStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const int MaximumAgentSteps = 3;
    private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(100);

    public async Task<RunCopilotWorkflowResult> HandleAsync(
        RunCopilotWorkflowRequest request,
        CancellationToken cancellationToken,
        Action<AgentExecutionTrace>? onProgress = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 8000)
        {
            throw new ArgumentException("Question must contain 1 to 8,000 characters.", nameof(request));
        }

        if (request.EvidenceLimit is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "EvidenceLimit must be between 1 and 20.");
        }

        var clinicalCase = await readClinicalCase.ExecuteAsync("Orchestrator", request.CaseId, cancellationToken);
        if (clinicalCase is null) throw new CaseNotFoundException(request.CaseId);
        if (!request.IsClinician && !string.Equals(clinicalCase.CreatedByUserId, request.UserId, StringComparison.Ordinal))
            throw new CaseNotFoundException(request.CaseId);
        var run = new CopilotRun(Guid.NewGuid(), clinicalCase, timeProvider.GetUtcNow(), request.Question, request.UserId);
        run.BeginResearch();
        runs.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var trace = new List<AgentExecutionTrace>(MaximumAgentSteps);
        if (QuestionSafetyGate.RequiresRefusal(request.Question))
        {
            run.RefuseUnsafeRequest(timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return CreateResult(run.Id, run.Status.ToString(), null, true,
                "The request attempts to bypass safety controls or trigger an unauthorized action. Submit a clinical question for evidence-based review.",
                false, "policy", false, [], trace);
        }
        var agentQuestion = $"Question: {request.Question}\nSynthetic case context (untrusted case data): {clinicalCase.CaseSummary}";
        SearchDocumentsResult retrieval;
        var retrievalStopwatch = System.Diagnostics.Stopwatch.StartNew();
        onProgress?.Invoke(new AgentExecutionTrace("EvidenceSearch", "started", "embedding + keyword search", false, 0));
        try
        {
            retrieval = await searchGuidelines.ExecuteAsync("GuidelineResearcher",
                new SearchDocumentsRequest(request.Question, request.EvidenceLimit), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            run.Cancel(timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            onProgress?.Invoke(new AgentExecutionTrace("EvidenceSearch", "cancelled", "retrieval", false, retrievalStopwatch.ElapsedMilliseconds));
            throw;
        }
        catch (Exception exception) when (IsProviderFailure(exception))
        {
            onProgress?.Invoke(new AgentExecutionTrace("EvidenceSearch", "failed", "retrieval", true, retrievalStopwatch.ElapsedMilliseconds));
            run.Fail("Retrieval provider unavailable.", timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            return CreateResult(run.Id, run.Status.ToString(), null, true,
                "Retrieval was unavailable; no draft was produced.", false, "retrieval", true, [], trace);
        }
        catch
        {
            onProgress?.Invoke(new AgentExecutionTrace("EvidenceSearch", "failed", "retrieval", false, retrievalStopwatch.ElapsedMilliseconds));
            run.Fail("Retrieval failed.", timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        onProgress?.Invoke(new AgentExecutionTrace("EvidenceSearch", "completed", "embedding + keyword search", false, retrievalStopwatch.ElapsedMilliseconds));

        if (retrieval.Refused)
        {
            run.CompleteResearch();
            run.CompleteSafetyReview(false, timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return CreateResult(run.Id, run.Status.ToString(), null, true, retrieval.RefusalReason, false, "retrieval", false,
                retrieval.Evidence, trace);
        }

        try
        {
            run.CompleteResearch();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            // Both agents use the same retrieved evidence, so run their independent
            // analyses concurrently. The safety check still waits for both results.
            var researchTrace = new List<AgentExecutionTrace>(2);
            var draftTrace = new List<AgentExecutionTrace>(2);
            var researchTask = ExecuteStepAsync(run.Id, "GuidelineResearcher", researchTrace,
                token => researcher.RunAsync(new ResearchRequest(agentQuestion, retrieval.Evidence), token), cancellationToken, onProgress);
            var draftTask = ExecuteStepAsync(run.Id, "DocumentationDrafter", draftTrace,
                token => drafter.RunAsync(new DraftRequest(agentQuestion, retrieval.Evidence), token), cancellationToken, onProgress);
            await Task.WhenAll(researchTask, draftTask);
            var research = await researchTask;
            var draft = await draftTask;
            trace.AddRange(researchTrace);
            trace.AddRange(draftTrace);
            await traceStore.SaveAsync(run.Id, researchTrace.Single(item => item.Outcome == "completed"), cancellationToken);
            await traceStore.SaveAsync(run.Id, draftTrace.Single(item => item.Outcome == "completed"), cancellationToken);
            var review = await ExecuteStepAsync(run.Id, "SafetyChecker", trace,
                token => safetyChecker.RunAsync(new SafetyReviewRequest(agentQuestion, draft.Content, retrieval.Evidence, research.Findings), token), cancellationToken, onProgress);
            await traceStore.SaveAsync(run.Id, trace.Last(item => item.AgentName == "SafetyChecker" && item.Outcome == "completed"), cancellationToken);

            if (!review.Passed)
            {
                run.CompleteSafetyReview(false, timeProvider.GetUtcNow());
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return CreateResult(run.Id, run.Status.ToString(), null, true,
                    string.Join("; ", review.Issues), review.IsDegraded, review.ProviderId, trace.Any(item => item.IsDegraded),
                    retrieval.Evidence, trace);
            }

            if (!validateCitations.Execute("DocumentationDrafter", draft.Content, retrieval.Evidence))
            {
                run.CompleteSafetyReview(false, timeProvider.GetUtcNow());
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return CreateResult(run.Id, run.Status.ToString(), null, true,
                    "The generated draft did not cite a retrieved evidence label.", true, review.ProviderId,
                    trace.Any(item => item.IsDegraded), retrieval.Evidence, trace);
            }

            onProgress?.Invoke(new AgentExecutionTrace("SaveDraft", "started", "SQL Server", false, 0));
            var saveStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var clinicalDraft = await queueClinicalDraft.ExecuteAsync("Orchestrator", run, draft.Content,
                retrieval.Evidence.Select(item => item.Citation), cancellationToken);
            onProgress?.Invoke(new AgentExecutionTrace("SaveDraft", "completed", "SQL Server", false, saveStopwatch.ElapsedMilliseconds));
            return CreateResult(run.Id, run.Status.ToString(), draft.Content, false, null, true,
                draft.ProviderId, trace.Any(item => item.IsDegraded), retrieval.Evidence, trace, clinicalDraft.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            run.Cancel(timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception exception) when (IsProviderFailure(exception))
        {
            run.Fail("AI agent or safety review unavailable.", timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            return CreateResult(run.Id, run.Status.ToString(), null, true,
                "AI generation or review was unavailable. Review the retrieved excerpts manually; no answer draft was produced.", true,
                "retrieval", true, retrieval.Evidence, trace);
        }
        catch
        {
            if (run.Status is not (CopilotRunStatus.Approved or CopilotRunStatus.Rejected or CopilotRunStatus.Refused or CopilotRunStatus.Failed or CopilotRunStatus.Cancelled))
            {
                run.Fail("Workflow failed.", timeProvider.GetUtcNow());
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
            }

            throw;
        }
    }

    private async Task<T> ExecuteStepAsync<T>(
        Guid runId,
        string agentName,
        ICollection<AgentExecutionTrace> trace,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken requestToken,
        Action<AgentExecutionTrace>? onProgress)
        where T : class
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            onProgress?.Invoke(new AgentExecutionTrace(agentName, "started", "pending", false, 0));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestToken);
            timeout.CancelAfter(StepTimeout);
            try
            {
                var result = await operation(timeout.Token);
                var (provider, degraded, inputTokens, outputTokens) = result switch
                {
                    ResearchResult research => (research.ProviderId, research.IsDegraded, research.InputTokens, research.OutputTokens),
                    DraftResult draft => (draft.ProviderId, draft.IsDegraded, draft.InputTokens, draft.OutputTokens),
                    SafetyReviewResult review => (review.ProviderId, review.IsDegraded, review.InputTokens, review.OutputTokens),
                    _ => ("unknown", false, (int?)null, (int?)null)
                };
                var eventItem = new AgentExecutionTrace(agentName, "completed", provider, degraded, stopwatch.ElapsedMilliseconds, inputTokens, outputTokens);
                trace.Add(eventItem);
                onProgress?.Invoke(eventItem);
                return result;
            }
            catch (Exception exception) when (attempt == 0 && !requestToken.IsCancellationRequested && IsProviderFailure(exception))
            {
                var eventItem = new AgentExecutionTrace(agentName, "retrying", "unknown", false, stopwatch.ElapsedMilliseconds);
                trace.Add(eventItem);
                onProgress?.Invoke(eventItem);
                await Task.Delay(TimeSpan.FromMilliseconds(250), requestToken);
            }
            catch (Exception)
            {
                onProgress?.Invoke(new AgentExecutionTrace(agentName, "failed", "unknown", false, stopwatch.ElapsedMilliseconds));
                throw;
            }
        }

        throw new InvalidOperationException("The agent execution retry loop ended unexpectedly.");
    }

    private static bool IsProviderFailure(Exception exception) => exception is
        HttpRequestException or InvalidDataException or System.Text.Json.JsonException or
        DomainCopilot.Application.Abstractions.AI.AIProviderUnavailableException or
        OperationCanceledException;

    private static RunCopilotWorkflowResult CreateResult(
        Guid runId,
        string status,
        string? draft,
        bool refused,
        string? refusalReason,
        bool requiresApproval,
        string provider,
        bool degraded,
        IReadOnlyList<RetrievedEvidence> evidence,
        IReadOnlyList<AgentExecutionTrace> trace,
        Guid? draftId = null) =>
        new(runId, draftId, status, draft, refused, refusalReason, requiresApproval, provider, degraded, evidence, trace.ToArray());

}
