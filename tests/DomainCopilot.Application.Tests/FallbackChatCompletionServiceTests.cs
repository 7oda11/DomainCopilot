using DomainCopilot.Application.Abstractions.AI;
using DomainCopilot.Application.AI;
using DomainCopilot.Application.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace DomainCopilot.Application.Tests;

public sealed class FallbackChatCompletionServiceTests
{
    [Fact]
    public async Task Uses_local_provider_and_marks_degraded_when_hosted_provider_is_unavailable()
    {
        var primary = new FakeChatCompletionProvider("gemini", (_, _) =>
            Task.FromException<ChatCompletionResponse>(new AIProviderUnavailableException("gemini", "offline")));
        var fallback = new FakeChatCompletionProvider("ollama", (_, _) => Task.FromResult(
            new ChatCompletionResponse("Grounded local response", "ollama", false, 10, 20)));
        var service = new FallbackChatCompletionService(primary, fallback, NullLogger<FallbackChatCompletionService>.Instance);

        var result = await service.CompleteAsync(new ChatCompletionRequest("system", "question"), CancellationToken.None);

        Assert.True(result.IsDegraded);
        Assert.Equal("ollama", result.ProviderId);
        Assert.Equal(1, primary.CallCount);
        Assert.Equal(1, fallback.CallCount);
    }

    [Fact]
    public async Task Does_not_fallback_when_the_caller_cancels()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var primary = new FakeChatCompletionProvider("gemini", (_, _) =>
            Task.FromCanceled<ChatCompletionResponse>(cancellation.Token));
        var fallback = new FakeChatCompletionProvider("ollama", (_, _) => Task.FromResult(
            new ChatCompletionResponse("response", "ollama", false, null, null)));
        var service = new FallbackChatCompletionService(primary, fallback, NullLogger<FallbackChatCompletionService>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CompleteAsync(
            new ChatCompletionRequest("system", "question"), cancellation.Token));
        Assert.Equal(0, fallback.CallCount);
    }
}
