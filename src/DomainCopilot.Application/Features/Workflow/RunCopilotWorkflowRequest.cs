namespace DomainCopilot.Application.Features.Workflow;

public sealed record RunCopilotWorkflowRequest(Guid CaseId, string Question, int EvidenceLimit = 8,
    string UserId = "system", bool IsClinician = true);
