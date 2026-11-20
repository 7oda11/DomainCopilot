using DomainCopilot.Domain.Approvals;
using DomainCopilot.Domain.Cases;
using DomainCopilot.Domain.Errors;
using DomainCopilot.Domain.Notes;
using DomainCopilot.Domain;

namespace DomainCopilot.Domain.Workflows;

public sealed class CopilotRun : IAggregateRoot
{
    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public CopilotRunStatus Status { get; private set; }
    public Guid? DraftId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private CopilotRun()
    {
    }

    public CopilotRun(Guid id, ClinicalCase clinicalCase, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainRuleViolationException("run.id.empty", "A run ID is required.");
        }

        ArgumentNullException.ThrowIfNull(clinicalCase);
        EnsureUtc(createdAtUtc);

        Id = id;
        CaseId = clinicalCase.Id;
        CreatedAtUtc = createdAtUtc;
        Status = CopilotRunStatus.Created;
    }

    public void BeginResearch()
    {
        EnsureStatus(CopilotRunStatus.Created);
        Status = CopilotRunStatus.Researching;
    }

    public void CompleteResearch()
    {
        EnsureStatus(CopilotRunStatus.Researching);
        Status = CopilotRunStatus.SafetyChecking;
    }

    public void CompleteSafetyReview(bool evidenceSupportsDraft, DateTimeOffset completedAtUtc)
    {
        EnsureStatus(CopilotRunStatus.SafetyChecking);

        if (evidenceSupportsDraft)
        {
            Status = CopilotRunStatus.Drafting;
            return;
        }

        CompleteAt(completedAtUtc);
        Status = CopilotRunStatus.Refused;
    }

    public void SubmitForApproval(ClinicalNoteDraft draft)
    {
        EnsureStatus(CopilotRunStatus.Drafting);
        ArgumentNullException.ThrowIfNull(draft);

        if (draft.RunId != Id)
        {
            throw new DomainRuleViolationException("run.draft.mismatch", "The draft must belong to this run.");
        }

        DraftId = draft.Id;
        Status = CopilotRunStatus.AwaitingApproval;
    }

    public void Approve(ApprovalRecord decision)
    {
        EnsureStatus(CopilotRunStatus.AwaitingApproval);
        EnsureDecisionMatchesDraft(decision);

        if (decision.Action is not (ApprovalAction.Approved or ApprovalAction.EditedAndApproved))
        {
            throw new DomainRuleViolationException("run.approval.action.invalid", "An approval decision is required to approve this run.");
        }

        CompleteAt(decision.DecidedAtUtc);
        Status = CopilotRunStatus.Approved;
    }

    public void Reject(ApprovalRecord decision)
    {
        EnsureStatus(CopilotRunStatus.AwaitingApproval);
        EnsureDecisionMatchesDraft(decision);

        if (decision.Action != ApprovalAction.Rejected)
        {
            throw new DomainRuleViolationException("run.approval.action.invalid", "A rejection decision is required to reject this run.");
        }

        CompleteAt(decision.DecidedAtUtc);
        Status = CopilotRunStatus.Rejected;
    }

    public void Fail(string reason, DateTimeOffset failedAtUtc)
    {
        EnsureNotTerminal();
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleViolationException("run.failure_reason.required", "A failure reason is required.");
        }

        CompleteAt(failedAtUtc);
        Status = CopilotRunStatus.Failed;
        FailureReason = reason.Trim();
    }

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        EnsureNotTerminal();
        CompleteAt(cancelledAtUtc);
        Status = CopilotRunStatus.Cancelled;
    }

    private void EnsureDecisionMatchesDraft(ApprovalRecord decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (DraftId != decision.DraftId)
        {
            throw new DomainRuleViolationException("run.approval.draft_mismatch", "The decision must apply to the draft submitted by this run.");
        }
    }

    private void EnsureStatus(CopilotRunStatus requiredStatus)
    {
        if (Status != requiredStatus)
        {
            throw new DomainRuleViolationException(
                "run.status.invalid_transition",
                $"This operation requires status {requiredStatus}; the run is {Status}.");
        }
    }

    private void EnsureNotTerminal()
    {
        if (Status is CopilotRunStatus.Approved or CopilotRunStatus.Rejected or
            CopilotRunStatus.Refused or CopilotRunStatus.Failed or CopilotRunStatus.Cancelled)
        {
            throw new DomainRuleViolationException("run.status.terminal", "A completed run cannot be changed.");
        }
    }

    private void CompleteAt(DateTimeOffset completedAtUtc)
    {
        EnsureUtc(completedAtUtc);
        if (completedAtUtc < CreatedAtUtc)
        {
            throw new DomainRuleViolationException("run.completed_at.invalid", "A run cannot complete before it was created.");
        }

        CompletedAtUtc = completedAtUtc;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainRuleViolationException("run.timestamp.invalid", "Timestamps must be UTC.");
        }
    }
}
