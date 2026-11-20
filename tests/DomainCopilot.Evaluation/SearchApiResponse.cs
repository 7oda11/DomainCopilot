namespace DomainCopilot.Evaluation;

public sealed record SearchApiResponse(
    bool Refused,
    string? RefusalReason,
    IReadOnlyList<SearchEvidence> Evidence);
