using DomainCopilot.Domain.Errors;

namespace DomainCopilot.Domain.Documents;

public sealed class SourceDocument
{
    public Guid Id { get; }
    public string SourceName { get; }
    public DocumentFormat Format { get; }
    public string Version { get; }
    public string ContentSha256 { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DocumentProcessingStatus Status { get; private set; }
    public string? FailureReason { get; private set; }

    public SourceDocument(
        Guid id,
        string sourceName,
        DocumentFormat format,
        string version,
        string contentSha256,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainRuleViolationException("document.id.empty", "A document ID is required.");
        }

        if (string.IsNullOrWhiteSpace(sourceName))
        {
            throw new DomainRuleViolationException("document.source.required", "A source name is required.");
        }

        if (!Enum.IsDefined(format))
        {
            throw new DomainRuleViolationException("document.format.invalid", "The document format is not supported.");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new DomainRuleViolationException("document.version.required", "A source version is required.");
        }

        if (!IsSha256(contentSha256))
        {
            throw new DomainRuleViolationException("document.fingerprint.invalid", "The document fingerprint must be a SHA-256 value.");
        }

        EnsureUtc(createdAtUtc, "document.created_at.invalid");

        Id = id;
        SourceName = sourceName.Trim();
        Format = format;
        Version = version.Trim();
        ContentSha256 = contentSha256.ToLowerInvariant();
        CreatedAtUtc = createdAtUtc;
        Status = DocumentProcessingStatus.Received;
    }

    public void BeginProcessing()
    {
        if (Status is not (DocumentProcessingStatus.Received or DocumentProcessingStatus.Failed))
        {
            throw new DomainRuleViolationException("document.processing.invalid_transition", "Only a received or failed document can be processed.");
        }

        Status = DocumentProcessingStatus.Processing;
        FailureReason = null;
    }

    public void MarkIndexed()
    {
        EnsureProcessing();
        Status = DocumentProcessingStatus.Indexed;
    }

    public void MarkFailed(string reason)
    {
        EnsureProcessing();
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleViolationException("document.failure_reason.required", "A failure reason is required.");
        }

        Status = DocumentProcessingStatus.Failed;
        FailureReason = reason.Trim();
    }

    private void EnsureProcessing()
    {
        if (Status != DocumentProcessingStatus.Processing)
        {
            throw new DomainRuleViolationException("document.processing.invalid_transition", "The document must be processing first.");
        }
    }

    private static bool IsSha256(string value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);

    private static void EnsureUtc(DateTimeOffset value, string code)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainRuleViolationException(code, "Timestamps must be UTC.");
        }
    }
}
