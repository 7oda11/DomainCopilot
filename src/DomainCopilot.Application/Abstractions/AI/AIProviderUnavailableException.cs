namespace DomainCopilot.Application.Abstractions.AI;

public sealed class AIProviderUnavailableException(string providerId, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string ProviderId { get; } = providerId;
}
