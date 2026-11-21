using DomainCopilot.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Identity;

namespace DomainCopilot.Infrastructure.Authentication;

public sealed class AspNetPasswordCredentialService : IPasswordCredentialService
{
    private readonly PasswordHasher<PasswordSubject> _hasher = new();
    private static readonly PasswordSubject Subject = new();

    public string HashPassword(string password) => _hasher.HashPassword(Subject, password);

    public bool VerifyPassword(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(Subject, passwordHash, password) is
            PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;

    private sealed class PasswordSubject { }
}
