using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DomainCopilot.Application.Abstractions.AI;
using Microsoft.Extensions.Configuration;

namespace DomainCopilot.Infrastructure.AI;

public sealed class GeminiChatCompletionProvider(IHttpClientFactory clients, IConfiguration configuration) : IChatCompletionProvider
{
    private readonly string _apiKey = configuration["AI:Gemini:ApiKey"] ?? configuration["GEMINI_API_KEY"] ?? string.Empty;
    private readonly string _endpoint = configuration["AI:Gemini:Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    private readonly string _model = configuration["AI:Gemini:Model"] ?? "gemini-2.5-flash";
    public string ProviderId => $"gemini:{_model}";

    public async Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new AIProviderUnavailableException(ProviderId, "Gemini is not configured; set GEMINI_API_KEY to enable the hosted provider.");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = JsonContent.Create(new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                },
                max_tokens = request.MaxOutputTokens,
                temperature = 0.1
            })
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        using var response = await clients.CreateClient(nameof(GeminiChatCompletionProvider)).SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = new HttpRequestException($"Gemini returned HTTP {(int)response.StatusCode}.");
            if ((int)response.StatusCode is >= 400 and < 500 && response.StatusCode is not System.Net.HttpStatusCode.Unauthorized and not System.Net.HttpStatusCode.Forbidden and not System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new InvalidOperationException("Gemini rejected the chat request; verify its format and limits.", error);
            }

            throw new AIProviderUnavailableException(ProviderId, error.Message, error);
        }

        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = payload.RootElement;
        var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content)) throw new AIProviderUnavailableException(ProviderId, "Gemini returned an empty response.");
        var usage = root.TryGetProperty("usage", out var usageElement) ? usageElement : default;
        return new ChatCompletionResponse(content, ProviderId, false,
            ReadOptionalInt(usage, "prompt_tokens"), ReadOptionalInt(usage, "completion_tokens"));
    }

    private static int? ReadOptionalInt(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var count)
            ? count
            : null;
}
