using DomainCopilot.Application.Abstractions.Documents;
using DomainCopilot.Domain.Documents;
using DomainCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DomainCopilot.Infrastructure.Documents;

public sealed class EfDocumentIngestionStore(AppDbContext dbContext) : IDocumentIngestionStore
{
    public Task<SourceDocument?> FindByFingerprintAsync(
        string fingerprint,
        string version,
        CancellationToken cancellationToken) => dbContext.SourceDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(document => document.ContentSha256 == fingerprint && document.Version == version, cancellationToken);

    public async Task SaveAsync(
        SourceDocument document,
        IReadOnlyCollection<DocumentChunk> chunks,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.SourceDocuments.Add(document);
        dbContext.DocumentChunks.AddRange(chunks);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(Guid documentId, CancellationToken cancellationToken) =>
        await dbContext.DocumentChunks.AsNoTracking().Where(chunk => chunk.DocumentId == documentId)
            .OrderBy(chunk => chunk.Ordinal).ToListAsync(cancellationToken);

    public async Task MarkIndexedAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.SourceDocuments.SingleAsync(item => item.Id == documentId, cancellationToken);
        document.MarkIndexed();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
