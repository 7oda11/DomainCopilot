using DomainCopilot.Application.Abstractions.AI;

namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FakeChatCompletionProvider(
    string providerId,
    Func<ChatCompletionRequest, CancellationToken, Task<ChatCompletionResponse>> complete) : IChatCompletionProvider
{
    public string ProviderId => providerId;
    public int CallCount { get; private set; }

    public async Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        CallCount++;
        return await complete(request, cancellationToken);
    }
}
