using DomainCopilot.Domain.Errors;
using DomainCopilot.Domain;

namespace DomainCopilot.Domain.Cases;

public sealed class ClinicalCase : IAggregateRoot
{
    public Guid Id { get; private set; }
    public string CaseSummary { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;

    private ClinicalCase()
    {
    }

    public ClinicalCase(Guid id, string caseSummary, DateTimeOffset createdAtUtc, string createdByUserId = "system")
    {
        if (id == Guid.Empty)
        {
            throw new DomainRuleViolationException("case.id.empty", "A case ID is required.");
        }

        if (string.IsNullOrWhiteSpace(caseSummary))
        {
            throw new DomainRuleViolationException("case.summary.required", "A case summary is required.");
        }

        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainRuleViolationException("case.created_at.invalid", "Timestamps must be UTC.");
        }

        Id = id;
        CaseSummary = caseSummary.Trim();
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = string.IsNullOrWhiteSpace(createdByUserId) ? "system" : createdByUserId.Trim();
    }
}
