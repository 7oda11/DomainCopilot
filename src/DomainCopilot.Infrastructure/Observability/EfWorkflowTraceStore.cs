using DomainCopilot.Application.Abstractions.Observability;
using DomainCopilot.Application.Features.Workflow;
using DomainCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DomainCopilot.Infrastructure.Observability;

public sealed class EfWorkflowTraceStore(AppDbContext db, IConfiguration configuration) : IWorkflowTraceStore
{
    public async Task SaveAsync(Guid runId, AgentExecutionTrace trace, CancellationToken cancellationToken)
    {
        var inputRate = configuration.GetValue<decimal?>("AI:GeminiCostPerMillionInputTokensUsd");
        var outputRate = configuration.GetValue<decimal?>("AI:GeminiCostPerMillionOutputTokensUsd");
        decimal? estimatedCost = string.Equals(trace.ProviderId, "gemini", StringComparison.OrdinalIgnoreCase)
            && inputRate.HasValue && outputRate.HasValue && trace.InputTokens.HasValue && trace.OutputTokens.HasValue
            ? (trace.InputTokens.Value * inputRate.Value + trace.OutputTokens.Value * outputRate.Value) / 1_000_000m
            : null;
        db.WorkflowTraceEntries.Add(new WorkflowTraceEntry
        {
            Id = Guid.NewGuid(), RunId = runId, AgentName = trace.AgentName, Outcome = trace.Outcome,
            ProviderId = trace.ProviderId, IsDegraded = trace.IsDegraded, ElapsedMilliseconds = trace.ElapsedMilliseconds,
            InputTokens = trace.InputTokens, OutputTokens = trace.OutputTokens, EstimatedCostUsd = estimatedCost,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgentExecutionTrace>> GetAsync(Guid runId, string userId, bool includeAll, CancellationToken cancellationToken) =>
        await db.WorkflowTraceEntries.AsNoTracking().Where(item => item.RunId == runId
                && (includeAll || db.CopilotRuns.Any(run => run.Id == item.RunId && run.CreatedByUserId == userId)))
            .OrderBy(item => item.CreatedAtUtc)
            .Select(item => new AgentExecutionTrace(item.AgentName, item.Outcome, item.ProviderId, item.IsDegraded,
                item.ElapsedMilliseconds, item.InputTokens, item.OutputTokens, item.EstimatedCostUsd))
            .ToArrayAsync(cancellationToken);
}
