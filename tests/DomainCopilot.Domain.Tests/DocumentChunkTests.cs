using DomainCopilot.Domain.Documents;
using DomainCopilot.Domain.Errors;

namespace DomainCopilot.Domain.Tests;

public sealed class DocumentChunkTests
{
    [Fact]
    public void Chunk_rejects_page_zero_because_source_pages_start_at_one()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() => new DocumentChunk(
            Guid.NewGuid(), Guid.NewGuid(), 0, "Evidence text", "Section 2", 0, "2026"));

        Assert.Equal("chunk.page.invalid", exception.Code);
    }

    [Fact]
    public void Chunk_keeps_exact_source_location_and_version()
    {
        var documentId = Guid.NewGuid();
        var chunk = new DocumentChunk(
            Guid.NewGuid(), documentId, 3, "  Evidence text  ", " Section 2 ", 14, "2026-01");

        Assert.Equal(documentId, chunk.DocumentId);
        Assert.Equal(3, chunk.Ordinal);
        Assert.Equal("Evidence text", chunk.Text);
        Assert.Equal("Section 2", chunk.Section);
        Assert.Equal(14, chunk.PageNumber);
        Assert.Equal("2026-01", chunk.SourceVersion);
    }
}
