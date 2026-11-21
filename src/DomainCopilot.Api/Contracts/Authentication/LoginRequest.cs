using System.ComponentModel.DataAnnotations;

namespace DomainCopilot.Api.Contracts.Authentication;

public sealed record LoginRequest(
    [param: Required, EmailAddress, MaxLength(320)] string Email,
    [param: Required, MinLength(1), MaxLength(128)] string Password);
