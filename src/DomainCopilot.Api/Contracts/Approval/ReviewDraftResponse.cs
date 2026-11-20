using DomainCopilot.Domain.Approvals;

namespace DomainCopilot.Api.Contracts.Approval;

public sealed record ReviewDraftResponse(Guid RunId, Guid DraftId, ApprovalAction Action, string Status, string? ApprovedContent);
