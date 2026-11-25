using DomainCopilot.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Infrastructure.Persistence.Configurations;

public sealed class ClinicalCaseConfiguration : IEntityTypeConfiguration<ClinicalCase>
{
    public void Configure(EntityTypeBuilder<ClinicalCase> builder)
    {
        builder.ToTable("ClinicalCases");
        builder.HasKey(clinicalCase => clinicalCase.Id);
        builder.Property(clinicalCase => clinicalCase.CaseSummary)
            .HasMaxLength(8000)
            .IsRequired();
        builder.Property(clinicalCase => clinicalCase.CreatedAtUtc).IsRequired();
        builder.Property(clinicalCase => clinicalCase.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasIndex(clinicalCase => new { clinicalCase.CreatedByUserId, clinicalCase.CreatedAtUtc });
    }
}
