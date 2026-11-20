namespace DomainCopilot.Application.Features.Agents;

public sealed class AgentToolPermissionException(string agentName, string toolName)
    : Exception($"Agent '{agentName}' is not allowed to invoke tool '{toolName}'.");
