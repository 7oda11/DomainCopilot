using System.Net.Http.Json;
using System.Text.Json;
using DomainCopilot.Application.Abstractions.Retrieval;
using Microsoft.Extensions.Configuration;

namespace DomainCopilot.Infrastructure.AI;

public sealed class OllamaEmbeddingGenerator(IHttpClientFactory clients, IConfiguration configuration) : IEmbeddingGenerator
{
    private readonly string _endpoint = configuration["Embedding:Ollama:Endpoint"] ?? "http://localhost:11434/api/embed";
    private readonly string _model = configuration["Embedding:Ollama:Model"] ?? "nomic-embed-text";
    public string ModelId => $"ollama:{_model}";

    public async Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Embedding input cannot be empty.", nameof(text));
        using var response = await clients.CreateClient(nameof(OllamaEmbeddingGenerator)).PostAsJsonAsync(_endpoint,
            new { model = _model, input = text }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Ollama embedding endpoint returned HTTP {(int)response.StatusCode}.");
        }

        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (!payload.RootElement.TryGetProperty("embeddings", out var embeddings) || embeddings.GetArrayLength() == 0)
        {
            throw new InvalidDataException("Ollama returned no embedding vector.");
        }

        var vector = embeddings[0].EnumerateArray().Select(value => value.GetSingle()).ToArray();
        ValidateVector(vector);
        return vector;
    }

    internal static void ValidateVector(float[] vector)
    {
        if (vector.Length == 0 || vector.Length > 16384 || vector.Any(value => !float.IsFinite(value)))
        {
            throw new InvalidDataException("The embedding vector is empty, too large, or contains a non-finite value.");
        }
    }
}
