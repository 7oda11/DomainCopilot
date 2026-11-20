using DomainCopilot.Application.Abstractions.Retrieval;

namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FakeVectorIndex : IVectorIndex
{
    public Dictionary<Guid, float[]> Vectors { get; } = [];
    public IReadOnlyList<VectorMatch> SearchResults { get; set; } = [];

    public Task UpsertAsync(Guid chunkId, string modelId, float[] vector, CancellationToken cancellationToken)
    {
        Vectors[chunkId] = vector;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorMatch>> SearchAsync(string modelId, float[] queryVector, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<VectorMatch>>(SearchResults.Take(limit).ToArray());
}
