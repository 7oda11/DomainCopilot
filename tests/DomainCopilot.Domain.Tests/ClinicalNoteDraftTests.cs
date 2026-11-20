using DomainCopilot.Domain.Errors;
using DomainCopilot.Domain.Notes;

namespace DomainCopilot.Domain.Tests;

public sealed class ClinicalNoteDraftTests
{
    [Fact]
    public void Clinical_note_without_evidence_is_rejected()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() => new ClinicalNoteDraft(
            Guid.NewGuid(), Guid.NewGuid(), "Draft text", [], DateTimeOffset.UtcNow));

        Assert.Equal("draft.citations.required", exception.Code);
    }
}
