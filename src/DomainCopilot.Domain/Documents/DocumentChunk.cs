using DomainCopilot.Domain.Errors;

namespace DomainCopilot.Domain.Documents;

public sealed class DocumentChunk
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int Ordinal { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string? Section { get; private set; }
    public int? PageNumber { get; private set; }
    public string SourceVersion { get; private set; } = string.Empty;

    private DocumentChunk()
    {
    }

    public DocumentChunk(
        Guid id,
        Guid documentId,
        int ordinal,
        string text,
        string? section,
        int? pageNumber,
        string sourceVersion)
    {
        if (id == Guid.Empty || documentId == Guid.Empty)
        {
            throw new DomainRuleViolationException("chunk.id.empty", "Chunk and source document IDs are required.");
        }

        if (ordinal < 0)
        {
            throw new DomainRuleViolationException("chunk.ordinal.invalid", "Chunk ordinal cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainRuleViolationException("chunk.text.required", "Chunk text is required.");
        }

        if (pageNumber is <= 0)
        {
            throw new DomainRuleViolationException("chunk.page.invalid", "Page numbers start at one.");
        }

        if (string.IsNullOrWhiteSpace(sourceVersion))
        {
            throw new DomainRuleViolationException("chunk.version.required", "The source version is required.");
        }

        Id = id;
        DocumentId = documentId;
        Ordinal = ordinal;
        Text = text.Trim();
        Section = string.IsNullOrWhiteSpace(section) ? null : section.Trim();
        PageNumber = pageNumber;
        SourceVersion = sourceVersion.Trim();
    }
}
