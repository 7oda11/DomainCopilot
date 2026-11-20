using DomainCopilot.Application.Abstractions.Retrieval;
using DomainCopilot.Application.Features.Retrieval;
using DomainCopilot.Application.Tests.Fakes;

namespace DomainCopilot.Application.Tests;

public sealed class HybridSearchHandlerTests
{
    [Fact]
    public async Task Fuses_dense_and_keyword_results_into_traceable_citations()
    {
        var text = "For adults, the guideline recommends checking blood pressure at each visit.";
        var chunkId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var store = new FakeDocumentRetrievalStore([new DocumentSearchCandidate(
            chunkId, documentId, "Synthetic guideline", "v1", text, "Screening", 4)]);
        var vectors = new FakeVectorIndex { SearchResults = [new VectorMatch(chunkId, 0.86)] };
        var handler = new HybridSearchHandler(new FakeEmbeddingGenerator(), vectors, store);

        var result = await handler.HandleAsync(new SearchDocumentsRequest("blood pressure screening", 3), CancellationToken.None);

        Assert.False(result.Refused);
        var citation = Assert.Single(result.Evidence).Citation;
        Assert.Equal(documentId, citation.DocumentId);
        Assert.Equal(chunkId, citation.ChunkId);
        Assert.Equal(4, citation.PageNumber);
        Assert.Equal(text, citation.QuotedText);
        Assert.Contains("blood", citation.QuotedText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refuses_when_no_retriever_returns_relevant_evidence()
    {
        var handler = new HybridSearchHandler(new FakeEmbeddingGenerator(), new FakeVectorIndex(), new FakeDocumentRetrievalStore([]));

        var result = await handler.HandleAsync(new SearchDocumentsRequest("unsourced clinical claim"), CancellationToken.None);

        Assert.True(result.Refused);
        Assert.Empty(result.Evidence);
        Assert.NotNull(result.RefusalReason);
    }
}
