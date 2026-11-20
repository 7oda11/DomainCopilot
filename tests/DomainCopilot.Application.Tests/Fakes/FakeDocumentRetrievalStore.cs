using DomainCopilot.Application.Abstractions.Retrieval;

namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FakeDocumentRetrievalStore(IReadOnlyList<DocumentSearchCandidate> candidates) : IDocumentRetrievalStore
{
    public Task<IReadOnlyList<DocumentSearchCandidate>> SearchKeywordsAsync(
        IReadOnlyCollection<string> terms, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DocumentSearchCandidate>>(candidates
            .Where(candidate => terms.Any(term => candidate.Text.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Take(limit).ToArray());

    public Task<IReadOnlyList<DocumentSearchCandidate>> GetByChunkIdsAsync(
        IReadOnlyCollection<Guid> chunkIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DocumentSearchCandidate>>(candidates.Where(candidate => chunkIds.Contains(candidate.ChunkId)).ToArray());
}
