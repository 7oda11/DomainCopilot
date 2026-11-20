using DomainCopilot.Domain.Notes;
using DomainCopilot.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Infrastructure.Persistence.Configurations;

public sealed class ClinicalNoteDraftConfiguration : IEntityTypeConfiguration<ClinicalNoteDraft>
{
    public void Configure(EntityTypeBuilder<ClinicalNoteDraft> builder)
    {
        builder.ToTable("ClinicalNoteDrafts");
        builder.HasKey(draft => draft.Id);
        builder.Property(draft => draft.Content).HasMaxLength(12000).IsRequired();
        builder.Property(draft => draft.CreatedAtUtc).IsRequired();
        builder.HasIndex(draft => draft.RunId).IsUnique();
        builder.HasOne<CopilotRun>().WithMany()
            .HasForeignKey(draft => draft.RunId).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsMany(draft => draft.Citations, citation =>
        {
            citation.ToTable("ClinicalNoteCitations");
            citation.WithOwner().HasForeignKey("DraftId");
            citation.Property<int>("Ordinal");
            citation.HasKey("DraftId", "Ordinal");
            citation.Property(item => item.DocumentId).IsRequired();
            citation.Property(item => item.ChunkId).IsRequired();
            citation.Property(item => item.SourceVersion).HasMaxLength(100).IsRequired();
            citation.Property(item => item.Section).HasMaxLength(500);
            citation.Property(item => item.PageNumber);
            citation.Property(item => item.QuotedText).HasMaxLength(12000).IsRequired();
        });
        builder.Navigation(draft => draft.Citations).HasField("_citations").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
