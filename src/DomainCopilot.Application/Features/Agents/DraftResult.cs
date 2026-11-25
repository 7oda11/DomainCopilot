namespace DomainCopilot.Application.Features.Agents;

public sealed record DraftResult(string Content, string ProviderId, bool IsDegraded, int? InputTokens = null, int? OutputTokens = null);
