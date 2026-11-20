using DomainCopilot.Domain.Documents;
using DomainCopilot.Domain.Errors;

namespace DomainCopilot.Domain.Tests;

public sealed class SourceDocumentTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Processing_must_finish_before_a_document_can_be_indexed()
    {
        var document = CreateDocument();

        var exception = Assert.Throws<DomainRuleViolationException>(document.MarkIndexed);

        Assert.Equal("document.processing.invalid_transition", exception.Code);
        document.BeginProcessing();
        document.MarkIndexed();
        Assert.Equal(DocumentProcessingStatus.Indexed, document.Status);
    }

    [Fact]
    public void Failed_document_can_be_retried_and_failure_is_cleared()
    {
        var document = CreateDocument();
        document.BeginProcessing();
        document.MarkFailed("Temporary extraction error");

        document.BeginProcessing();

        Assert.Equal(DocumentProcessingStatus.Processing, document.Status);
        Assert.Null(document.FailureReason);
    }

    [Fact]
    public void Fingerprint_must_be_a_sha256_hex_value()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() => new SourceDocument(
            Guid.NewGuid(), "guideline.pdf", DocumentFormat.Pdf, "2026", "not-a-hash", CreatedAtUtc));

        Assert.Equal("document.fingerprint.invalid", exception.Code);
    }

    private static SourceDocument CreateDocument() => new(
        Guid.NewGuid(), "Synthetic guideline.pdf", DocumentFormat.Pdf, "2026-01", new string('a', 64), CreatedAtUtc);
}
