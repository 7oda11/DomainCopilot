using DomainCopilot.Domain.Approvals;

namespace DomainCopilot.Application.Features.Approval;

public sealed record ReviewClinicalDraftRequest(
    Guid DraftId,
    ApprovalAction Action,
    string? Comment = null,
    string? ApprovedContent = null);
