namespace DomainCopilot.Application.Features.Authentication;

public sealed class EmailAlreadyRegisteredException() : Exception("An account with this email already exists.");
