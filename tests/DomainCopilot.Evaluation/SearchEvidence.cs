namespace DomainCopilot.Evaluation;

public sealed record SearchEvidence(
    Guid DocumentId,
    Guid ChunkId,
    string SourceName,
    string SourceVersion,
    string? Section,
    int? PageNumber,
    string QuotedText,
    double Score);
