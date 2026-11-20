using DomainCopilot.Application.Abstractions.Documents;
using DomainCopilot.Application.Features.Ingestion;
using DomainCopilot.Application.Tests.Fakes;
using DomainCopilot.Domain.Documents;

namespace DomainCopilot.Application.Tests;

public sealed class IngestDocumentHandlerTests
{
    [Fact]
    public async Task Ingests_and_chunks_each_extracted_page_with_its_metadata()
    {
        var extractor = new FakeDocumentTextExtractor([new ExtractedPage(3, new string('a', 220), "Safety")]);
        var store = new FakeDocumentIngestionStore();
        var embeddings = new FakeEmbeddingGenerator();
        var vectors = new FakeVectorIndex();
        var handler = new IngestDocumentHandler([extractor], store, embeddings, vectors, new DocumentChunker(120, 20),
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(new IngestDocumentRequest("guideline.pdf", "2026", DocumentFormat.Pdf, [1, 2, 3]), CancellationToken.None);

        Assert.False(result.WasAlreadyIndexed);
        Assert.Equal(2, result.ChunkCount);
        Assert.All(store.Chunks, chunk => Assert.Equal(3, chunk.PageNumber));
        Assert.All(store.Chunks, chunk => Assert.Equal("Safety", chunk.Section));
        Assert.All(store.Chunks, chunk => Assert.True(chunk.Text.Length <= 120));
        Assert.Equal(2, embeddings.CallCount);
        Assert.Equal(2, vectors.Vectors.Count);
    }

    [Fact]
    public async Task Repeated_content_and_version_are_idempotent()
    {
        var extractor = new FakeDocumentTextExtractor([new ExtractedPage(1, "A clinical guideline section.")]);
        var store = new FakeDocumentIngestionStore();
        var embeddings = new FakeEmbeddingGenerator();
        var handler = new IngestDocumentHandler([extractor], store, embeddings, new FakeVectorIndex(), new DocumentChunker(), TimeProvider.System);
        var request = new IngestDocumentRequest("guideline.pdf", "2026", DocumentFormat.Pdf, [4, 5, 6]);

        var first = await handler.HandleAsync(request, CancellationToken.None);
        var second = await handler.HandleAsync(request, CancellationToken.None);

        Assert.False(first.WasAlreadyIndexed);
        Assert.True(second.WasAlreadyIndexed);
        Assert.Equal(first.DocumentId, second.DocumentId);
        Assert.Equal(1, extractor.ExtractionCount);
        Assert.Equal(1, embeddings.CallCount);
    }

    [Fact]
    public async Task Rejects_empty_documents_before_calling_an_extractor()
    {
        var extractor = new FakeDocumentTextExtractor([]);
        var handler = new IngestDocumentHandler([extractor], new FakeDocumentIngestionStore(), new FakeEmbeddingGenerator(), new FakeVectorIndex(), new DocumentChunker(), TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new IngestDocumentRequest("empty.pdf", "1", DocumentFormat.Pdf, []), CancellationToken.None));
        Assert.Equal(0, extractor.ExtractionCount);
    }
}
