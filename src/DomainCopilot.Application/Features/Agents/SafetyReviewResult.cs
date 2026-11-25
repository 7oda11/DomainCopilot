namespace DomainCopilot.Application.Features.Agents;

public sealed record SafetyReviewResult(bool Passed, IReadOnlyList<string> Issues, string ProviderId, bool IsDegraded,
    int? InputTokens = null, int? OutputTokens = null);
