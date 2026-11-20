using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Domain.Notes;

namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FakeClinicalNoteDraftRepository(ClinicalNoteDraft? initialDraft = null) : IRepository<ClinicalNoteDraft>
{
    public ClinicalNoteDraft? AddedDraft { get; private set; } = initialDraft;

    public Task<ClinicalNoteDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(AddedDraft?.Id == id ? AddedDraft : null);

    public void Add(ClinicalNoteDraft aggregate) => AddedDraft = aggregate;
}
