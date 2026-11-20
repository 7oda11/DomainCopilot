using DomainCopilot.Application.Features.Retrieval;

namespace DomainCopilot.Application.Features.Workflow;

public sealed record RunCopilotWorkflowResult(
    Guid RunId,
    Guid? DraftId,
    string Status,
    string? Draft,
    bool Refused,
    string? RefusalReason,
    bool RequiresClinicianApproval,
    string ActiveProvider,
    bool IsDegraded,
    IReadOnlyList<RetrievedEvidence> Evidence,
    IReadOnlyList<AgentExecutionTrace> Trace);
