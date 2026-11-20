namespace DomainCopilot.Application.Abstractions.AI;

public sealed record ChatCompletionResponse(
    string Content,
    string ProviderId,
    bool IsDegraded,
    int? InputTokens,
    int? OutputTokens);
