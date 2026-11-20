using System.Text.RegularExpressions;
using DomainCopilot.Application.Abstractions.Retrieval;
using DomainCopilot.Domain.Documents;
using DomainCopilot.Domain.Evidence;

namespace DomainCopilot.Application.Features.Retrieval;

public sealed partial class HybridSearchHandler(
    IEmbeddingGenerator embeddingGenerator,
    IVectorIndex vectorIndex,
    IDocumentRetrievalStore documentStore)
{
    private const int FusionRankConstant = 60;
    private const double MinimumCosineSimilarity = 0.15;

    public async Task<SearchDocumentsResult> HandleAsync(SearchDocumentsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Query)) throw new ArgumentException("A search query is required.", nameof(request));
        if (request.Limit is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(request), "Limit must be between 1 and 20.");

        var terms = TokenPattern().Matches(request.Query.ToLowerInvariant()).Select(match => match.Value)
            .Where(term => term.Length > 1).Distinct(StringComparer.Ordinal).Take(20).ToArray();
        if (terms.Length == 0)
        {
            return new SearchDocumentsResult(true, "The query contains no searchable terms.", []);
        }

        var vector = await embeddingGenerator.GenerateAsync(request.Query, cancellationToken);
        var denseMatches = await vectorIndex.SearchAsync(embeddingGenerator.ModelId, vector, request.Limit * 4, cancellationToken);
        var keywordMatches = await documentStore.SearchKeywordsAsync(terms, request.Limit * 4, cancellationToken);

        var ranks = new Dictionary<Guid, double>();
        for (var index = 0; index < denseMatches.Count; index++)
        {
            var match = denseMatches[index];
            if (match.Similarity < MinimumCosineSimilarity) continue;
            ranks[match.ChunkId] = ranks.GetValueOrDefault(match.ChunkId) + 1d / (FusionRankConstant + index + 1);
        }

        for (var index = 0; index < keywordMatches.Count; index++)
        {
            var candidate = keywordMatches[index];
            ranks[candidate.ChunkId] = ranks.GetValueOrDefault(candidate.ChunkId) + 1d / (FusionRankConstant + index + 1);
        }

        if (ranks.Count == 0)
        {
            return new SearchDocumentsResult(true, "No sufficiently relevant source evidence was found.", []);
        }

        var ordered = ranks.OrderByDescending(pair => pair.Value).Take(request.Limit).ToArray();
        var chunks = await documentStore.GetByChunkIdsAsync(ordered.Select(item => item.Key).ToArray(), cancellationToken);
        var byId = chunks.ToDictionary(chunk => chunk.ChunkId);
        var evidence = new List<RetrievedEvidence>();
        foreach (var match in ordered)
        {
            if (!byId.TryGetValue(match.Key, out var chunk)) continue;
            var domainChunk = new DocumentChunk(chunk.ChunkId, chunk.DocumentId, 0, chunk.Text,
                chunk.Section, chunk.PageNumber, chunk.SourceVersion);
            evidence.Add(new RetrievedEvidence(new EvidenceCitation(domainChunk, chunk.Text), chunk.SourceName, match.Value));
        }

        return evidence.Count == 0
            ? new SearchDocumentsResult(true, "No retrievable source evidence was found.", [])
            : new SearchDocumentsResult(false, null, evidence);
    }

    [GeneratedRegex("[\\p{L}\\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
