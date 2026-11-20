namespace DomainCopilot.Application.Features.Workflow;

public sealed record RunCopilotWorkflowRequest(Guid CaseId, string Question, int EvidenceLimit = 8);
