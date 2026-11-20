namespace DomainCopilot.Api.Contracts.Retrieval;

public sealed record SearchEvidenceResponse(
    Guid DocumentId,
    Guid ChunkId,
    string SourceName,
    string SourceVersion,
    string? Section,
    int? PageNumber,
    string QuotedText,
    double Score);
