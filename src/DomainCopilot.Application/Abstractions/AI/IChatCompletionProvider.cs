namespace DomainCopilot.Application.Abstractions.AI;

public interface IChatCompletionProvider
{
    string ProviderId { get; }
    Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken);
}
