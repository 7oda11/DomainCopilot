namespace DomainCopilot.Application.Features.Authentication;

public sealed record RegisterUserResult(Guid Id, string Email, string Role);
