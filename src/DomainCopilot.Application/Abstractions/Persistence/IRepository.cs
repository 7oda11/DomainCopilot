using DomainCopilot.Domain;

namespace DomainCopilot.Application.Abstractions.Persistence;

public interface IRepository<TAggregate>
    where TAggregate : class, IAggregateRoot
{
    Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(TAggregate aggregate);
}
