using System.ComponentModel.DataAnnotations;

namespace DomainCopilot.Api.Contracts.Authentication;

public sealed record LoginRequest(
    [property: Required, EmailAddress, MaxLength(320)] string Email,
    [property: Required, MinLength(1), MaxLength(128)] string Password);
