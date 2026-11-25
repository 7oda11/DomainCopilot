using DomainCopilot.Domain.Workflows;
using DomainCopilot.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Infrastructure.Persistence.Configurations;

public sealed class WorkflowTraceEntryConfiguration : IEntityTypeConfiguration<WorkflowTraceEntry>
{
    public void Configure(EntityTypeBuilder<WorkflowTraceEntry> builder)
    {
        builder.ToTable("WorkflowTraceEntries");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.AgentName).HasMaxLength(80).IsRequired();
        builder.Property(item => item.Outcome).HasMaxLength(24).IsRequired();
        builder.Property(item => item.ProviderId).HasMaxLength(80).IsRequired();
        builder.Property(item => item.EstimatedCostUsd).HasPrecision(18, 8);
        builder.HasIndex(item => new { item.RunId, item.CreatedAtUtc });
        builder.HasOne<CopilotRun>().WithMany().HasForeignKey(item => item.RunId).OnDelete(DeleteBehavior.Cascade);
    }
}
