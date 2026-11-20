using DomainCopilot.Application.Abstractions.Documents;
using DomainCopilot.Domain.Documents;

namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FakeDocumentIngestionStore : IDocumentIngestionStore
{
    private readonly List<SourceDocument> _documents = [];
    private readonly Dictionary<Guid, IReadOnlyList<DocumentChunk>> _chunksByDocument = [];
    public IReadOnlyCollection<DocumentChunk> Chunks { get; private set; } = [];

    public Task<SourceDocument?> FindByFingerprintAsync(string fingerprint, string version, CancellationToken cancellationToken) =>
        Task.FromResult(_documents.SingleOrDefault(document => document.ContentSha256 == fingerprint && document.Version == version));

    public Task SaveAsync(SourceDocument document, IReadOnlyCollection<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        _documents.Add(document);
        Chunks = chunks;
        _chunksByDocument[document.Id] = chunks.ToArray();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(Guid documentId, CancellationToken cancellationToken) =>
        Task.FromResult(_chunksByDocument.GetValueOrDefault(documentId, []));

    public Task MarkIndexedAsync(Guid documentId, CancellationToken cancellationToken)
    {
        _documents.Single(document => document.Id == documentId).MarkIndexed();
        return Task.CompletedTask;
    }
}
