using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Domain.Cases;

namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FakeClinicalCaseRepository(ClinicalCase? clinicalCase) : IRepository<ClinicalCase>
{
    public Task<ClinicalCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(clinicalCase?.Id == id ? clinicalCase : null);

    public void Add(ClinicalCase aggregate) =>
        throw new NotSupportedException("This fake only supports case lookup.");
}
