namespace DomainCopilot.Evaluation;

public sealed record EvaluationCase(
    string Id,
    string Category,
    string Query,
    bool ExpectedRefusal,
    IReadOnlyList<string> ExpectedSources,
    IReadOnlyList<IReadOnlyList<string>> SupportClaims,
    IReadOnlyList<string> Tags);
