using System.Linq.Expressions;
using DomainCopilot.Application.Abstractions.Retrieval;
using DomainCopilot.Domain.Documents;
using DomainCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DomainCopilot.Infrastructure.Documents;

public sealed class EfDocumentRetrievalStore(AppDbContext dbContext) : IDocumentRetrievalStore
{
    public async Task<IReadOnlyList<DocumentSearchCandidate>> SearchKeywordsAsync(
        IReadOnlyCollection<string> terms,
        int limit,
        CancellationToken cancellationToken)
    {
        if (terms.Count == 0) return [];
        var predicate = BuildContainsPredicate(terms);
        var candidates = await (from chunk in dbContext.DocumentChunks.AsNoTracking().Where(predicate)
                                join document in dbContext.SourceDocuments.AsNoTracking() on chunk.DocumentId equals document.Id
                                where document.Status == DocumentProcessingStatus.Indexed
                                select new DocumentSearchCandidate(chunk.Id, document.Id, document.SourceName,
                                    document.Version, chunk.Text, chunk.Section, chunk.PageNumber))
            .Take(Math.Clamp(limit * 10, 1, 2000))
            .ToListAsync(cancellationToken);

        return candidates.Select(candidate => candidate with
            {
                KeywordScore = terms.Count(term => candidate.Text.Contains(term, StringComparison.OrdinalIgnoreCase))
            })
            .Where(candidate => candidate.KeywordScore > 0)
            .OrderByDescending(candidate => candidate.KeywordScore)
            .Take(limit)
            .ToArray();
    }

    public async Task<IReadOnlyList<DocumentSearchCandidate>> GetByChunkIdsAsync(
        IReadOnlyCollection<Guid> chunkIds,
        CancellationToken cancellationToken)
    {
        if (chunkIds.Count == 0) return [];
        return await (from chunk in dbContext.DocumentChunks.AsNoTracking()
                      join document in dbContext.SourceDocuments.AsNoTracking() on chunk.DocumentId equals document.Id
                      where chunkIds.Contains(chunk.Id) && document.Status == DocumentProcessingStatus.Indexed
                      select new DocumentSearchCandidate(chunk.Id, document.Id, document.SourceName,
                          document.Version, chunk.Text, chunk.Section, chunk.PageNumber))
            .ToListAsync(cancellationToken);
    }

    private static Expression<Func<DocumentChunk, bool>> BuildContainsPredicate(IReadOnlyCollection<string> terms)
    {
        var parameter = Expression.Parameter(typeof(DocumentChunk), "chunk");
        var text = Expression.Property(parameter, nameof(DocumentChunk.Text));
        Expression body = Expression.Constant(false);
        foreach (var term in terms.Where(term => !string.IsNullOrWhiteSpace(term)))
        {
            var contains = Expression.Call(text, nameof(string.Contains), Type.EmptyTypes, Expression.Constant(term));
            body = Expression.OrElse(body, contains);
        }

        return Expression.Lambda<Func<DocumentChunk, bool>>(body, parameter);
    }
}
