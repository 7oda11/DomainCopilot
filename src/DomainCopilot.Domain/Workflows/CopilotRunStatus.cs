namespace DomainCopilot.Domain.Workflows;

public enum CopilotRunStatus
{
    Created,
    Researching,
    SafetyChecking,
    Drafting,
    AwaitingApproval,
    Approved,
    Rejected,
    Refused,
    Failed,
    Cancelled
}
