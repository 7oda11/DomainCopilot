namespace DomainCopilot.Application.Abstractions.AI;

public interface IChatCompletionService
{
    Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken);
}
