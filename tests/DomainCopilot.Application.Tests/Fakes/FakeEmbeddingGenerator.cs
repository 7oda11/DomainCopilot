using DomainCopilot.Application.Abstractions.Retrieval;

namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FakeEmbeddingGenerator : IEmbeddingGenerator
{
    public string ModelId => "fake-embedding-v1";
    public int CallCount { get; private set; }

    public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(new[] { (float)text.Length, 1f });
    }
}
