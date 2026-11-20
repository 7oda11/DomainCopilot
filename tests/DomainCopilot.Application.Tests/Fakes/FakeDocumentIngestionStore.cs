using DomainCopilot.Application.Abstractions.Documents;
using DomainCopilot.Domain.Documents;

namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FakeDocumentIngestionStore : IDocumentIngestionStore
{
    private readonly List<SourceDocument> _documents = [];
    public IReadOnlyCollection<DocumentChunk> Chunks { get; private set; } = [];

    public Task<SourceDocument?> FindByFingerprintAsync(string fingerprint, string version, CancellationToken cancellationToken) =>
        Task.FromResult(_documents.SingleOrDefault(document => document.ContentSha256 == fingerprint && document.Version == version));

    public Task SaveAsync(SourceDocument document, IReadOnlyCollection<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        _documents.Add(document);
        Chunks = chunks;
        return Task.CompletedTask;
    }
}
