using System.ComponentModel.DataAnnotations;

namespace DomainCopilot.Api.Contracts.Authentication;

public sealed record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(320)] string Email,
    [property: Required, MinLength(12), MaxLength(128)] string Password);
