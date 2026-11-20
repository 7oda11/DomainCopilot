namespace DomainCopilot.Application.Features.Retrieval;

public sealed record SearchDocumentsResult(
    bool Refused,
    string? RefusalReason,
    IReadOnlyList<RetrievedEvidence> Evidence);
