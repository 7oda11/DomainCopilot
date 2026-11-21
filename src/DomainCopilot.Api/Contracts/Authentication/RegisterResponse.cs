namespace DomainCopilot.Api.Contracts.Authentication;

public sealed record RegisterResponse(Guid Id, string Email, string Role);
