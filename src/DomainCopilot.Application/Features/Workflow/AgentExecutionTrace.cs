namespace DomainCopilot.Application.Features.Workflow;

public sealed record AgentExecutionTrace(string AgentName, string Outcome, string ProviderId, bool IsDegraded,
    long ElapsedMilliseconds, int? InputTokens = null, int? OutputTokens = null, decimal? EstimatedCostUsd = null);
