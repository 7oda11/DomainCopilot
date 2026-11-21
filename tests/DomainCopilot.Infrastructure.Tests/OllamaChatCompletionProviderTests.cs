using System.Net;
using System.Text;
using System.Text.Json;
using DomainCopilot.Application.Abstractions.AI;
using DomainCopilot.Infrastructure.AI;
using Microsoft.Extensions.Configuration;

namespace DomainCopilot.Infrastructure.Tests;

public sealed class OllamaChatCompletionProviderTests
{
    [Fact]
    public async Task Complete_disables_thinking_and_returns_visible_model_content()
    {
        var handler = new RecordingHandler();
        using var client = new HttpClient(handler);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AI:Ollama:BaseUrl"] = "http://localhost:11434",
            ["AI:Ollama:Model"] = "qwen3:8b"
        }).Build();
        var provider = new OllamaChatCompletionProvider(new FixedHttpClientFactory(client), configuration);

        var result = await provider.CompleteAsync(
            new ChatCompletionRequest("Use the supplied evidence.", "Summarize this synthetic workflow.", 64),
            CancellationToken.None);

        Assert.Equal("http://localhost:11434/api/chat", handler.RequestUri?.ToString());
        Assert.NotNull(handler.RequestBody);
        using var request = JsonDocument.Parse(handler.RequestBody!);
        Assert.False(request.RootElement.GetProperty("think").GetBoolean());
        Assert.Equal("qwen3:8b", request.RootElement.GetProperty("model").GetString());
        Assert.Equal("Synthetic answer.", result.Content);
        Assert.Equal("ollama:qwen3:8b", result.ProviderId);
        Assert.Equal(2, result.InputTokens);
        Assert.Equal(3, result.OutputTokens);
    }

    private sealed class FixedHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"model":"qwen3:8b","message":{"content":"Synthetic answer."},"prompt_eval_count":2,"eval_count":3}""",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
