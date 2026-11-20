namespace DomainCopilot.Application.Abstractions.Retrieval;

public sealed record DocumentSearchCandidate(
    Guid ChunkId,
    Guid DocumentId,
    string SourceName,
    string SourceVersion,
    string Text,
    string? Section,
    int? PageNumber,
    double KeywordScore = 0);
