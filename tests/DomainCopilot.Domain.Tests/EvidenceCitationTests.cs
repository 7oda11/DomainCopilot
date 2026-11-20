using DomainCopilot.Domain.Documents;
using DomainCopilot.Domain.Errors;
using DomainCopilot.Domain.Evidence;

namespace DomainCopilot.Domain.Tests;

public sealed class EvidenceCitationTests
{
    [Fact]
    public void Citation_must_quote_text_from_its_exact_chunk()
    {
        var chunk = new DocumentChunk(
            Guid.NewGuid(), Guid.NewGuid(), 0, "Use only supported evidence.", "Safety", 2, "v1");

        var citation = new EvidenceCitation(chunk, "supported evidence");

        Assert.Equal(chunk.Id, citation.ChunkId);
        Assert.Equal(chunk.DocumentId, citation.DocumentId);
        Assert.Equal(chunk.PageNumber, citation.PageNumber);
    }

    [Fact]
    public void Citation_rejects_a_quote_absent_from_the_chunk()
    {
        var chunk = new DocumentChunk(
            Guid.NewGuid(), Guid.NewGuid(), 0, "Use only supported evidence.", "Safety", 2, "v1");

        var exception = Assert.Throws<DomainRuleViolationException>(() => new EvidenceCitation(chunk, "unsupported claim"));

        Assert.Equal("citation.quote.not_in_chunk", exception.Code);
    }
}
