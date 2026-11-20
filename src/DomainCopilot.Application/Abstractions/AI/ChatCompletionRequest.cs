namespace DomainCopilot.Application.Abstractions.AI;

public sealed record ChatCompletionRequest(string SystemPrompt, string UserPrompt, int MaxOutputTokens = 1200);
