using DomainCopilot.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Infrastructure.Persistence.Configurations;

public sealed class SourceDocumentConfiguration : IEntityTypeConfiguration<SourceDocument>
{
    public void Configure(EntityTypeBuilder<SourceDocument> builder)
    {
        builder.ToTable("SourceDocuments");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.SourceName).HasMaxLength(260).IsRequired();
        builder.Property(document => document.Format).HasConversion<string>().HasMaxLength(16);
        builder.Property(document => document.Version).HasMaxLength(100).IsRequired();
        builder.Property(document => document.ContentSha256).HasMaxLength(64).IsRequired();
        builder.Property(document => document.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(document => document.FailureReason).HasMaxLength(2000);
        builder.Property(document => document.CreatedAtUtc).IsRequired();
        builder.Property(document => document.UploadedByUserId).HasMaxLength(200).IsRequired();
        builder.HasIndex(document => new { document.UploadedByUserId, document.CreatedAtUtc });
        builder.HasIndex(document => new { document.ContentSha256, document.Version }).IsUnique();
    }
}
