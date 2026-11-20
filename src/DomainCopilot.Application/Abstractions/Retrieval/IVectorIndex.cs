namespace DomainCopilot.Application.Abstractions.Retrieval;

public interface IVectorIndex
{
    Task UpsertAsync(Guid chunkId, string modelId, float[] vector, CancellationToken cancellationToken);

    Task<IReadOnlyList<VectorMatch>> SearchAsync(string modelId, float[] queryVector, int limit, CancellationToken cancellationToken);
}
