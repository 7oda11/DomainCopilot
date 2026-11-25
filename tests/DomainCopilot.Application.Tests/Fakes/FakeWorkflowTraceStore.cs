using DomainCopilot.Application.Abstractions.Observability;
using DomainCopilot.Application.Features.Workflow;

namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FakeWorkflowTraceStore : IWorkflowTraceStore
{
    private readonly List<(Guid RunId, AgentExecutionTrace Trace)> _items = [];

    public Task SaveAsync(Guid runId, AgentExecutionTrace trace, CancellationToken cancellationToken)
    {
        _items.Add((runId, trace));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AgentExecutionTrace>> GetAsync(Guid runId, string userId, bool includeAll, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AgentExecutionTrace>>(_items.Where(item => item.RunId == runId).Select(item => item.Trace).ToArray());
}
