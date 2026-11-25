namespace DomainCopilot.Api.Contracts.Copilot;

public sealed record WorkflowTraceResponse(string AgentName, string Outcome, string ProviderId, bool IsDegraded,
    long ElapsedMilliseconds, int? InputTokens = null, int? OutputTokens = null, decimal? EstimatedCostUsd = null);
