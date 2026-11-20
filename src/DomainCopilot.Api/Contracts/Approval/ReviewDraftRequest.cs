using DomainCopilot.Domain.Approvals;

namespace DomainCopilot.Api.Contracts.Approval;

public sealed class ReviewDraftRequest
{
    public required ApprovalAction Action { get; init; }
    public string? Comment { get; init; }
    public string? ApprovedContent { get; init; }
}
