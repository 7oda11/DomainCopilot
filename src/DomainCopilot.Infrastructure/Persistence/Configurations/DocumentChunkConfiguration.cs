using DomainCopilot.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Infrastructure.Persistence.Configurations;

public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("DocumentChunks");
        builder.HasKey(chunk => chunk.Id);
        builder.Property(chunk => chunk.Text).HasMaxLength(12000).IsRequired();
        builder.Property(chunk => chunk.Section).HasMaxLength(500);
        builder.Property(chunk => chunk.SourceVersion).HasMaxLength(100).IsRequired();
        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.Ordinal }).IsUnique();
        builder.HasOne<SourceDocument>()
            .WithMany()
            .HasForeignKey(chunk => chunk.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
