using System.Net.Http.Json;
using System.Text.Json;
using DomainCopilot.Application.Abstractions.AI;
using Microsoft.Extensions.Configuration;

namespace DomainCopilot.Infrastructure.AI;

public sealed class OllamaChatCompletionProvider(IHttpClientFactory clients, IConfiguration configuration) : IChatCompletionProvider
{
    private readonly string _baseUrl = (configuration["AI:Ollama:BaseUrl"] ?? "http://localhost:11434").TrimEnd('/');
    private readonly string _model = configuration["AI:Ollama:Model"] ?? "qwen3:8b";
    public string ProviderId => $"ollama:{_model}";

    public async Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        using var response = await clients.CreateClient(nameof(OllamaChatCompletionProvider)).PostAsJsonAsync(
            $"{_baseUrl}/api/chat", new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                },
                stream = false,
                options = new { temperature = 0.1, num_predict = request.MaxOutputTokens }
            }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new AIProviderUnavailableException(ProviderId, $"Ollama returned HTTP {(int)response.StatusCode}.");
        }

        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = payload.RootElement;
        var content = root.GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content)) throw new AIProviderUnavailableException(ProviderId, "Ollama returned an empty response.");
        return new ChatCompletionResponse(content, ProviderId, false,
            ReadOptionalInt(root, "prompt_eval_count"), ReadOptionalInt(root, "eval_count"));
    }

    private static int? ReadOptionalInt(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var count) ? count : null;
}
