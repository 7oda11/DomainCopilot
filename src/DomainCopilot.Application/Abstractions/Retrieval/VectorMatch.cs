namespace DomainCopilot.Application.Abstractions.Retrieval;

public sealed record VectorMatch(Guid ChunkId, double Similarity);
