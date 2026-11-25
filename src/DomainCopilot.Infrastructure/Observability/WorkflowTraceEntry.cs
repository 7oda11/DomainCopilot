namespace DomainCopilot.Infrastructure.Observability;

public sealed class WorkflowTraceEntry
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public bool IsDegraded { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
