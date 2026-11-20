using DomainCopilot.Domain.Errors;

namespace DomainCopilot.Domain.Approvals;

public sealed class ApprovalRecord
{
    public Guid Id { get; }
    public Guid DraftId { get; }
    public string ReviewerId { get; }
    public ApprovalAction Action { get; }
    public string? Comment { get; }
    public string? ApprovedContent { get; }
    public DateTimeOffset DecidedAtUtc { get; }

    public ApprovalRecord(
        Guid id,
        Guid draftId,
        string reviewerId,
        ApprovalAction action,
        DateTimeOffset decidedAtUtc,
        string? comment = null,
        string? approvedContent = null)
    {
        if (id == Guid.Empty || draftId == Guid.Empty)
        {
            throw new DomainRuleViolationException("approval.id.empty", "Approval and draft IDs are required.");
        }

        if (string.IsNullOrWhiteSpace(reviewerId))
        {
            throw new DomainRuleViolationException("approval.reviewer.required", "The reviewing clinician is required.");
        }

        if (!Enum.IsDefined(action))
        {
            throw new DomainRuleViolationException("approval.action.invalid", "The approval action is not supported.");
        }

        if (action == ApprovalAction.Rejected && string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainRuleViolationException("approval.rejection_reason.required", "A rejection reason is required.");
        }

        if (action == ApprovalAction.EditedAndApproved && string.IsNullOrWhiteSpace(approvedContent))
        {
            throw new DomainRuleViolationException("approval.edited_content.required", "Edited content is required for edit-and-approve.");
        }

        if (action != ApprovalAction.EditedAndApproved && !string.IsNullOrWhiteSpace(approvedContent))
        {
            throw new DomainRuleViolationException("approval.edited_content.unexpected", "Approved content is only supplied when edits are approved.");
        }

        if (decidedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainRuleViolationException("approval.timestamp.invalid", "Timestamps must be UTC.");
        }

        Id = id;
        DraftId = draftId;
        ReviewerId = reviewerId.Trim();
        Action = action;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        ApprovedContent = string.IsNullOrWhiteSpace(approvedContent) ? null : approvedContent.Trim();
        DecidedAtUtc = decidedAtUtc;
    }
}
