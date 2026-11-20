using DomainCopilot.Domain.Documents;
using DomainCopilot.Domain.Errors;

namespace DomainCopilot.Domain.Evidence;

public sealed class EvidenceCitation
{
    public Guid DocumentId { get; }
    public Guid ChunkId { get; }
    public string SourceVersion { get; }
    public string? Section { get; }
    public int? PageNumber { get; }
    public string QuotedText { get; }

    public EvidenceCitation(DocumentChunk chunk, string quotedText)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        if (string.IsNullOrWhiteSpace(quotedText) ||
            !chunk.Text.Contains(quotedText, StringComparison.Ordinal))
        {
            throw new DomainRuleViolationException("citation.quote.not_in_chunk", "The quoted evidence must appear in its cited chunk.");
        }

        DocumentId = chunk.DocumentId;
        ChunkId = chunk.Id;
        SourceVersion = chunk.SourceVersion;
        Section = chunk.Section;
        PageNumber = chunk.PageNumber;
        QuotedText = quotedText.Trim();
    }
}
