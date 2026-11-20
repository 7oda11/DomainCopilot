using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Domain.Workflows;

namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FakeCopilotRunRepository(CopilotRun? initialRun = null) : IRepository<CopilotRun>
{
    public CopilotRun? AddedRun { get; private set; } = initialRun;

    public Task<CopilotRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(AddedRun?.Id == id ? AddedRun : null);

    public void Add(CopilotRun aggregate) => AddedRun = aggregate;
}
