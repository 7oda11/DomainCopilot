namespace DomainCopilot.Evaluation;

public sealed record EvaluationSummary(
    DateTimeOffset ExecutedAtUtc,
    string Endpoint,
    int TotalCases,
    double? RetrievalHitRate,
    double? GroundednessRate,
    double RefusalCorrectnessRate,
    int AdversarialCases,
    int PromptInjectionCases,
    IReadOnlyList<CaseEvaluation> Cases,
    IReadOnlyList<string> Limitations);
