using DomainCopilot.Domain.Errors;
using DomainCopilot.Domain.Evidence;

namespace DomainCopilot.Domain.Notes;

public sealed class ClinicalNoteDraft
{
    public Guid Id { get; }
    public Guid RunId { get; }
    public string Content { get; }
    public IReadOnlyList<EvidenceCitation> Citations { get; }
    public DateTimeOffset CreatedAtUtc { get; }

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
        Citations = Array.AsReadOnly(citationList);
        CreatedAtUtc = createdAtUtc;
    }
}
