using DomainCopilot.Domain.Approvals;
using DomainCopilot.Domain.Notes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Infrastructure.Persistence.Configurations;

public sealed class ApprovalRecordConfiguration : IEntityTypeConfiguration<ApprovalRecord>
{
    public void Configure(EntityTypeBuilder<ApprovalRecord> builder)
    {
        builder.ToTable("ApprovalRecords");
        builder.HasKey(approval => approval.Id);
        builder.Property(approval => approval.ReviewerId).HasMaxLength(200).IsRequired();
        builder.Property(approval => approval.Action).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(approval => approval.Comment).HasMaxLength(2000);
        builder.Property(approval => approval.ApprovedContent).HasMaxLength(12000);
        builder.Property(approval => approval.DecidedAtUtc).IsRequired();
        builder.HasIndex(approval => approval.DraftId).IsUnique();
        builder.HasOne<ClinicalNoteDraft>().WithMany()
            .HasForeignKey(approval => approval.DraftId).OnDelete(DeleteBehavior.Restrict);
    }
}
