using DomainCopilot.Domain.Cases;
using DomainCopilot.Domain.Documents;
using DomainCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DomainCopilot.Infrastructure.Tests;

public sealed class RelationalModelTests
{
    [Fact]
    public void Documents_and_chunks_have_a_unique_idempotency_key_and_ordered_chunk_key()
    {
        using var context = CreateContext();
        var document = context.Model.FindEntityType(typeof(SourceDocument))!;
        var chunk = context.Model.FindEntityType(typeof(DocumentChunk))!;

        Assert.Equal("SourceDocuments", document.GetTableName());
        Assert.Contains(document.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(SourceDocument.ContentSha256), nameof(SourceDocument.Version)]));
        Assert.Contains(chunk.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(DocumentChunk.DocumentId), nameof(DocumentChunk.Ordinal)]));
    }

    [Fact]
    public void Document_chunks_are_deleted_with_their_source_document()
    {
        using var context = CreateContext();
        var chunk = context.Model.FindEntityType(typeof(DocumentChunk))!;
        var relationship = Assert.Single(chunk.GetForeignKeys());

        Assert.Equal(typeof(SourceDocument), relationship.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, relationship.DeleteBehavior);
    }

    [Fact]
    public void Clinical_case_maps_to_a_named_relational_table()
    {
        using var context = CreateContext();

        var caseEntity = context.Model.FindEntityType(typeof(ClinicalCase));

        Assert.NotNull(caseEntity);
        Assert.Equal("ClinicalCases", caseEntity.GetTableName());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=DomainCopilotTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AppDbContext(options);
    }
}
