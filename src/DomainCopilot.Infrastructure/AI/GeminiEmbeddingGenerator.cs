using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DomainCopilot.Application.Abstractions.Retrieval;
using Microsoft.Extensions.Configuration;

namespace DomainCopilot.Infrastructure.AI;

public sealed class GeminiEmbeddingGenerator(IHttpClientFactory clients, IConfiguration configuration) : IEmbeddingGenerator
{
    private readonly string _apiKey = configuration["Embedding:Gemini:ApiKey"] ?? configuration["GEMINI_API_KEY"] ?? string.Empty;
    private readonly string _baseUrl = (configuration["Embedding:Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/openai").TrimEnd('/');
    private readonly string _model = configuration["Embedding:Gemini:Model"] ?? "gemini-embedding-001";
    public string ModelId => $"gemini:{_model}";

    public async Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) throw new InvalidOperationException("Set Embedding:Gemini:ApiKey or GEMINI_API_KEY to use Gemini embeddings.");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/embeddings")
        {
            Content = JsonContent.Create(new { model = _model, input = text })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        using var response = await clients.CreateClient(nameof(GeminiEmbeddingGenerator)).SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini embedding endpoint returned HTTP {(int)response.StatusCode}.");
        }

        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var vector = payload.RootElement.GetProperty("data")[0].GetProperty("embedding")
            .EnumerateArray().Select(value => value.GetSingle()).ToArray();
        OllamaEmbeddingGenerator.ValidateVector(vector);
        return vector;
    }
}
