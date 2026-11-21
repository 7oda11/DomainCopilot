using System.ComponentModel.DataAnnotations;

namespace DomainCopilot.Api.Contracts.Authentication;

public sealed record RegisterRequest(
    [param: Required, EmailAddress, MaxLength(320)] string Email,
    [param: Required, MinLength(12), MaxLength(128)] string Password);
