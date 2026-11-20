using DomainCopilot.Api.Contracts.Retrieval;

namespace DomainCopilot.Api.Contracts.Copilot;

public sealed record CreateDraftResponse(
    Guid RunId,
    Guid? DraftId,
    string Status,
    string? Draft,
    bool Refused,
    string? RefusalReason,
    bool RequiresClinicianApproval,
    string ActiveProvider,
    bool IsDegraded,
    IReadOnlyList<SearchEvidenceResponse> Evidence,
    IReadOnlyList<WorkflowTraceResponse> Trace);
