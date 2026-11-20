namespace DomainCopilot.Evaluation;

public sealed record CaseEvaluation(
    string Id,
    string Category,
    bool ExpectedRefusal,
    bool ActualRefusal,
    bool RefusalCorrect,
    bool RetrievalHit,
    int GroundedClaims,
    int TotalClaims,
    int CitationCount,
    IReadOnlyList<string> RetrievedSources,
    IReadOnlyList<string> Notes);
