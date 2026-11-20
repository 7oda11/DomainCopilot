using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Domain;
using DomainCopilot.Infrastructure.Persistence;

namespace DomainCopilot.Infrastructure.Repositories;

public sealed class EfRepository<TAggregate>(AppDbContext dbContext) : IRepository<TAggregate>
    where TAggregate : class, IAggregateRoot
{
    public Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<TAggregate>().FindAsync(new object?[] { id }, cancellationToken).AsTask();

    public void Add(TAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        dbContext.Set<TAggregate>().Add(aggregate);
    }
}
