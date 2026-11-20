namespace DomainCopilot.Application.Abstractions.Retrieval;

public interface IDocumentRetrievalStore
{
    Task<IReadOnlyList<DocumentSearchCandidate>> SearchKeywordsAsync(
        IReadOnlyCollection<string> terms,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentSearchCandidate>> GetByChunkIdsAsync(
        IReadOnlyCollection<Guid> chunkIds,
        CancellationToken cancellationToken);
}
