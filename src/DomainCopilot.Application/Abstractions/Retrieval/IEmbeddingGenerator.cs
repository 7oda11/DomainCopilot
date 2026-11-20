namespace DomainCopilot.Application.Abstractions.Retrieval;

public interface IEmbeddingGenerator
{
    string ModelId { get; }
    Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken);
}
