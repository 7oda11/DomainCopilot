namespace DomainCopilot.Application.Features.Authentication;

public sealed record LoginUserResult(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc);
