using DomainCopilot.Application.Abstractions.AI;
using DomainCopilot.Application.AI;
using DomainCopilot.Application.Features.Agents;
using DomainCopilot.Application.Features.Approval;
using DomainCopilot.Application.Tests.Fakes;
using DomainCopilot.Domain.Approvals;
using DomainCopilot.Domain.Cases;
using DomainCopilot.Domain.Documents;
using DomainCopilot.Domain.Evidence;
using DomainCopilot.Domain.Notes;
using DomainCopilot.Domain.Workflows;
using Microsoft.Extensions.Logging.Abstractions;

namespace DomainCopilot.Application.Tests;

public sealed class ReviewClinicalDraftHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Approval_is_audited_and_completes_the_run()
    {
        var context = CreatePendingContext();
        var handler = CreateHandler(context, "unused");

        var result = await handler.HandleAsync(
            new ReviewClinicalDraftRequest(context.Draft.Id, ApprovalAction.Approved), "clinician-1", CancellationToken.None);

        Assert.Equal("Approved", result.Status);
        Assert.Equal(ApprovalAction.Approved, context.Approvals.AddedApproval?.Action);
        Assert.Equal("clinician-1", context.Approvals.AddedApproval?.ReviewerId);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Rejection_requires_reason_and_moves_the_run_to_rejected()
    {
        var context = CreatePendingContext();
        var handler = CreateHandler(context, "unused");

        var result = await handler.HandleAsync(
            new ReviewClinicalDraftRequest(context.Draft.Id, ApprovalAction.Rejected, "Source is outdated."),
            "clinician-1", CancellationToken.None);

        Assert.Equal("Rejected", result.Status);
        Assert.Equal("Source is outdated.", context.Approvals.AddedApproval?.Comment);
    }

    [Fact]
    public async Task Edited_content_is_blocked_when_safety_checker_rejects_it()
    {
        var context = CreatePendingContext();
        var handler = CreateHandler(context, "{\"passed\":false,\"issues\":[\"Unsupported claim\"]}");

        var exception = await Assert.ThrowsAsync<DomainCopilot.Domain.Errors.DomainRuleViolationException>(() => handler.HandleAsync(
            new ReviewClinicalDraftRequest(context.Draft.Id, ApprovalAction.EditedAndApproved, ApprovedContent: "Unsafe edit [E1]."),
            "clinician-1", CancellationToken.None));

        Assert.Equal("approval.edited_content.unsafe", exception.Code);
        Assert.Null(context.Approvals.AddedApproval);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
        Assert.Equal(CopilotRunStatus.AwaitingApproval, context.Run.Status);
    }

    private static ReviewClinicalDraftHandler CreateHandler(
        (ClinicalNoteDraft Draft, CopilotRun Run, FakeClinicalNoteDraftRepository Drafts,
            FakeCopilotRunRepository Runs, FakeApprovalRecordRepository Approvals, FakeUnitOfWork UnitOfWork) context,
        string safetyJson)
    {
        var provider = new FakeChatCompletionProvider("ollama", (_, _) => Task.FromResult(
            new ChatCompletionResponse(safetyJson, "ollama", false, null, null)));
        var chat = new FallbackChatCompletionService(provider, provider, NullLogger<FallbackChatCompletionService>.Instance);
        return new ReviewClinicalDraftHandler(context.Drafts, context.Runs, context.Approvals,
            context.UnitOfWork, new SafetyCheckerAgent(chat), new FixedTimeProvider(Now));
    }

    private static (ClinicalNoteDraft Draft, CopilotRun Run, FakeClinicalNoteDraftRepository Drafts,
        FakeCopilotRunRepository Runs, FakeApprovalRecordRepository Approvals, FakeUnitOfWork UnitOfWork) CreatePendingContext()
    {
        var clinicalCase = new ClinicalCase(Guid.NewGuid(), "Synthetic case", Now);
        var run = new CopilotRun(Guid.NewGuid(), clinicalCase, Now);
        run.BeginResearch();
        run.CompleteResearch();
        run.CompleteSafetyReview(true, Now);
        var chunk = new DocumentChunk(Guid.NewGuid(), Guid.NewGuid(), 0, "The guideline supports screening at each visit.", "Screening", 1, "v1");
        var draft = new ClinicalNoteDraft(Guid.NewGuid(), run.Id, "Screening at each visit [E1].", [new EvidenceCitation(chunk, chunk.Text)], Now);
        run.SubmitForApproval(draft);
        var approvals = new FakeApprovalRecordRepository();
        var unitOfWork = new FakeUnitOfWork();
        return (draft, run, new FakeClinicalNoteDraftRepository(draft),
            new FakeCopilotRunRepository(run), approvals, unitOfWork);
    }
}
