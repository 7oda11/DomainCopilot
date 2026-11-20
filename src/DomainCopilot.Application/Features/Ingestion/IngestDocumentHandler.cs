using System.Security.Cryptography;
using DomainCopilot.Application.Abstractions.Documents;
using DomainCopilot.Application.Abstractions.Retrieval;
using DomainCopilot.Domain.Documents;

namespace DomainCopilot.Application.Features.Ingestion;

public sealed class IngestDocumentHandler(
    IEnumerable<IDocumentTextExtractor> extractors,
    IDocumentIngestionStore store,
    IEmbeddingGenerator embeddingGenerator,
    IVectorIndex vectorIndex,
    DocumentChunker chunker,
    TimeProvider timeProvider)
{
    private const int MaximumContentBytes = 25 * 1024 * 1024;

    public async Task<IngestDocumentResult> HandleAsync(
        IngestDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.SourceName) || string.IsNullOrWhiteSpace(request.Version))
        {
            throw new ArgumentException("Source name and version are required.", nameof(request));
        }

        if (request.Content is not { Length: > 0 and <= MaximumContentBytes })
        {
            throw new ArgumentException("Document content must be between 1 byte and 25 MiB.", nameof(request));
        }

        var extractor = extractors.FirstOrDefault(candidate => candidate.Supports(request.Format))
            ?? throw new NotSupportedException($"No extractor is registered for {request.Format}.");
        var fingerprint = Convert.ToHexString(SHA256.HashData(request.Content)).ToLowerInvariant();
        var existing = await store.FindByFingerprintAsync(fingerprint, request.Version, cancellationToken);
        if (existing?.Status == DocumentProcessingStatus.Indexed)
        {
            return new IngestDocumentResult(existing.Id, true, 0);
        }

        if (existing is not null)
        {
            var pendingChunks = await store.GetChunksAsync(existing.Id, cancellationToken);
            await IndexChunksAsync(pendingChunks, cancellationToken);
            await store.MarkIndexedAsync(existing.Id, cancellationToken);
            return new IngestDocumentResult(existing.Id, true, pendingChunks.Count);
        }

        await using var stream = new MemoryStream(request.Content, writable: false);
        var pages = await extractor.ExtractAsync(stream, request.Format, cancellationToken);
        if (pages.Count > 500 || pages.Sum(page => (long)page.Text.Length) > 5_000_000)
        {
            throw new InvalidDataException("The extracted document exceeds the 500-page or 5-million-character processing limit.");
        }

        var chunkTexts = chunker.Chunk(pages);
        if (chunkTexts.Count == 0)
        {
            throw new InvalidDataException("The document contains no extractable text.");
        }

        var document = new SourceDocument(
            Guid.NewGuid(), request.SourceName, request.Format, request.Version,
            fingerprint, timeProvider.GetUtcNow());
        document.BeginProcessing();
        var chunks = chunkTexts.Select((chunk, index) => new DocumentChunk(
            Guid.NewGuid(), document.Id, index, chunk.Text, chunk.Section,
            chunk.PageNumber, document.Version)).ToArray();
        await store.SaveAsync(document, chunks, cancellationToken);
        await IndexChunksAsync(chunks, cancellationToken);
        await store.MarkIndexedAsync(document.Id, cancellationToken);
        return new IngestDocumentResult(document.Id, false, chunks.Length);
    }

    private async Task IndexChunksAsync(IReadOnlyCollection<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var vector = await embeddingGenerator.GenerateAsync(chunk.Text, cancellationToken);
            await vectorIndex.UpsertAsync(chunk.Id, embeddingGenerator.ModelId, vector, cancellationToken);
        }
    }
}
