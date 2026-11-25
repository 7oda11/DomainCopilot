using DomainCopilot.Application.Features.Workflow;

namespace DomainCopilot.Application.Abstractions.Observability;

public interface IWorkflowTraceStore
{
    Task SaveAsync(Guid runId, AgentExecutionTrace trace, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentExecutionTrace>> GetAsync(Guid runId, string userId, bool includeAll, CancellationToken cancellationToken);
}
