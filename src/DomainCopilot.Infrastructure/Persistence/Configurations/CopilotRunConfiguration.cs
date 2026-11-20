using DomainCopilot.Domain.Cases;
using DomainCopilot.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Infrastructure.Persistence.Configurations;

public sealed class CopilotRunConfiguration : IEntityTypeConfiguration<CopilotRun>
{
    public void Configure(EntityTypeBuilder<CopilotRun> builder)
    {
        builder.ToTable("CopilotRuns");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(run => run.FailureReason).HasMaxLength(2000);
        builder.Property(run => run.CreatedAtUtc).IsRequired();
        builder.HasIndex(run => new { run.CaseId, run.CreatedAtUtc });
        builder.HasOne<ClinicalCase>()
            .WithMany()
            .HasForeignKey(run => run.CaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
