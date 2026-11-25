namespace DomainCopilot.Application.Features.Agents;

public sealed record ResearchResult(string Findings, string ProviderId, bool IsDegraded, int? InputTokens = null, int? OutputTokens = null);
