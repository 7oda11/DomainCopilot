using DomainCopilot.Domain.Approvals;
using DomainCopilot.Domain.Cases;
using DomainCopilot.Domain.Documents;
using DomainCopilot.Domain.Errors;
using DomainCopilot.Domain.Evidence;
using DomainCopilot.Domain.Notes;
using DomainCopilot.Domain.Workflows;

namespace DomainCopilot.Domain.Tests;

public sealed class CopilotRunTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Run_requires_safety_review_and_clinician_approval_before_completion()
    {
        var (run, draft) = CreateRunAndDraft();
        run.BeginResearch();
        run.CompleteResearch();
        run.CompleteSafetyReview(evidenceSupportsDraft: true, CreatedAtUtc.AddMinutes(1));
        run.SubmitForApproval(draft);

        Assert.Equal(CopilotRunStatus.AwaitingApproval, run.Status);

        var approval = new ApprovalRecord(
            Guid.NewGuid(), draft.Id, "clinician-1", ApprovalAction.Approved, CreatedAtUtc.AddMinutes(2));
        run.Approve(approval);

        Assert.Equal(CopilotRunStatus.Approved, run.Status);
        Assert.Equal(approval.DecidedAtUtc, run.CompletedAtUtc);
    }

    [Fact]
    public void Run_refuses_when_safety_review_has_insufficient_evidence()
    {
        var run = CreateRun();
        run.BeginResearch();
        run.CompleteResearch();

        run.CompleteSafetyReview(evidenceSupportsDraft: false, CreatedAtUtc.AddMinutes(1));

        Assert.Equal(CopilotRunStatus.Refused, run.Status);
        Assert.NotNull(run.CompletedAtUtc);
        Assert.Throws<DomainRuleViolationException>(run.BeginResearch);
    }

    [Fact]
    public void Run_cannot_approve_a_decision_for_another_draft()
    {
        var (run, draft) = CreateRunAndDraft();
        run.BeginResearch();
        run.CompleteResearch();
        run.CompleteSafetyReview(evidenceSupportsDraft: true, CreatedAtUtc.AddMinutes(1));
        run.SubmitForApproval(draft);
        var decision = new ApprovalRecord(
            Guid.NewGuid(), Guid.NewGuid(), "clinician-1", ApprovalAction.Approved, CreatedAtUtc.AddMinutes(2));

        var exception = Assert.Throws<DomainRuleViolationException>(() => run.Approve(decision));

        Assert.Equal("run.approval.draft_mismatch", exception.Code);
    }

    [Fact]
    public void Invalid_completion_time_does_not_partially_change_run_state()
    {
        var run = CreateRun();
        run.BeginResearch();

        Assert.Throws<DomainRuleViolationException>(() => run.Fail(
            "Provider failed", CreatedAtUtc.AddMinutes(1).ToOffset(TimeSpan.FromHours(2))));

        Assert.Equal(CopilotRunStatus.Researching, run.Status);
        Assert.Null(run.CompletedAtUtc);
    }

    private static CopilotRun CreateRun() => new(
        Guid.NewGuid(),
        new ClinicalCase(Guid.NewGuid(), "Synthetic case summary", CreatedAtUtc),
        CreatedAtUtc);

    private static (CopilotRun Run, ClinicalNoteDraft Draft) CreateRunAndDraft()
    {
        var run = CreateRun();
        var chunk = new DocumentChunk(Guid.NewGuid(), Guid.NewGuid(), 0, "The guidance supports review.", "Section 1", 1, "v1");
        var citation = new EvidenceCitation(chunk, "guidance supports review");
        var draft = new ClinicalNoteDraft(
            Guid.NewGuid(), run.Id, "Draft for clinician review.", [citation], CreatedAtUtc.AddMinutes(1));

        return (run, draft);
    }
}
