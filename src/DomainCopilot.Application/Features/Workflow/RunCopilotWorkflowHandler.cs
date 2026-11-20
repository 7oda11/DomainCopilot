using System.Net.Http;
using System.Text.RegularExpressions;
using DomainCopilot.Application.Features.Agents;
using DomainCopilot.Application.Features.Retrieval;
using DomainCopilot.Application.Abstractions.Persistence;
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
        var run = new CopilotRun(Guid.NewGuid(), clinicalCase, timeProvider.GetUtcNow());
        run.BeginResearch();
        runs.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var trace = new List<AgentExecutionTrace>(MaximumAgentSteps);
        var agentQuestion = $"Question: {request.Question}\nSynthetic case context (untrusted case data): {clinicalCase.CaseSummary}";
        SearchDocumentsResult retrieval;
        try
        {
            retrieval = await searchGuidelines.ExecuteAsync("GuidelineResearcher",
                new SearchDocumentsRequest(request.Question, request.EvidenceLimit), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            run.Cancel(timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception exception) when (IsProviderFailure(exception))
        {
            run.Fail("Retrieval provider unavailable.", timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            return CreateResult(run.Id, run.Status.ToString(), null, true,
                "Retrieval was unavailable; no draft was produced.", false, "retrieval", true, [], trace);
        }
        catch
        {
            run.Fail("Retrieval failed.", timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }

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
            var research = await ExecuteStepAsync("GuidelineResearcher", trace,
                token => researcher.RunAsync(new ResearchRequest(agentQuestion, retrieval.Evidence), token), cancellationToken, onProgress);
            var draft = await ExecuteStepAsync("DocumentationDrafter", trace,
                token => drafter.RunAsync(new DraftRequest(agentQuestion, research.Findings, retrieval.Evidence), token), cancellationToken, onProgress);
            var review = await ExecuteStepAsync("SafetyChecker", trace,
                token => safetyChecker.RunAsync(new SafetyReviewRequest(agentQuestion, draft.Content, retrieval.Evidence), token), cancellationToken, onProgress);

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

            var clinicalDraft = await queueClinicalDraft.ExecuteAsync("Orchestrator", run, draft.Content,
                retrieval.Evidence.Select(item => item.Citation), cancellationToken);
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

    private static async Task<T> ExecuteStepAsync<T>(
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
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestToken);
            timeout.CancelAfter(StepTimeout);
            try
            {
                var result = await operation(timeout.Token);
                var (provider, degraded) = result switch
                {
                    ResearchResult research => (research.ProviderId, research.IsDegraded),
                    DraftResult draft => (draft.ProviderId, draft.IsDegraded),
                    SafetyReviewResult review => (review.ProviderId, review.IsDegraded),
                    _ => ("unknown", false)
                };
                var eventItem = new AgentExecutionTrace(agentName, "completed", provider, degraded, stopwatch.ElapsedMilliseconds);
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
