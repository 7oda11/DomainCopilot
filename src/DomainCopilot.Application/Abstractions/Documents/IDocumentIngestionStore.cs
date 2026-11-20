using DomainCopilot.Domain.Documents;

namespace DomainCopilot.Application.Abstractions.Documents;

public interface IDocumentIngestionStore
{
    Task<SourceDocument?> FindByFingerprintAsync(
        string fingerprint,
        string version,
        CancellationToken cancellationToken);

    Task SaveAsync(
        SourceDocument document,
        IReadOnlyCollection<DocumentChunk> chunks,
        CancellationToken cancellationToken);
}
