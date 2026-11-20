using DomainCopilot.Domain.Approvals;

namespace DomainCopilot.Application.Features.Approval;

public sealed record ReviewClinicalDraftResult(
    Guid RunId,
    Guid DraftId,
    ApprovalAction Action,
    string Status,
    string? ApprovedContent);
