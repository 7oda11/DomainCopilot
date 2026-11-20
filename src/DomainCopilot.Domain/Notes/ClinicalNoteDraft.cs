using DomainCopilot.Domain.Errors;
using DomainCopilot.Domain.Evidence;
using DomainCopilot.Domain;

namespace DomainCopilot.Domain.Notes;

public sealed class ClinicalNoteDraft : IAggregateRoot
{
    private readonly List<EvidenceCitation> _citations = [];

    public Guid Id { get; private set; }
    public Guid RunId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public IReadOnlyList<EvidenceCitation> Citations => _citations.AsReadOnly();
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ClinicalNoteDraft()
    {
    }

    public ClinicalNoteDraft(
        Guid id,
        Guid runId,
        string content,
        IEnumerable<EvidenceCitation> citations,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || runId == Guid.Empty)
        {
            throw new DomainRuleViolationException("draft.id.empty", "Draft and run IDs are required.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainRuleViolationException("draft.content.required", "Draft content is required.");
        }

        ArgumentNullException.ThrowIfNull(citations);
        var citationList = citations.ToArray();
        if (citationList.Length == 0)
        {
            throw new DomainRuleViolationException("draft.citations.required", "A clinical note draft requires supporting citations.");
        }

        if (citationList.Any(citation => citation is null))
        {
            throw new DomainRuleViolationException("draft.citation.invalid", "Citations cannot contain null values.");
        }

        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainRuleViolationException("draft.created_at.invalid", "Timestamps must be UTC.");
        }

        Id = id;
        RunId = runId;
        Content = content.Trim();
        _citations.AddRange(citationList);
        CreatedAtUtc = createdAtUtc;
    }
}
