using DomainCopilot.Domain.Errors;

namespace DomainCopilot.Domain.Users;

public sealed class UserAccount : IAggregateRoot
{
    private UserAccount()
    {
    }

    private UserAccount(Guid id, string email, string normalizedEmail, string passwordHash,
        string role, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = AccountRoles.Researcher;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new DomainRuleViolationException("user.email.invalid", "A valid email address is required.");
        }

        return email.Trim().ToUpperInvariant();
    }

    public static UserAccount CreateResearcher(string email, string passwordHash, DateTimeOffset createdAtUtc)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainRuleViolationException("user.password_hash.required", "A password hash is required.");
        }

        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainRuleViolationException("user.created_at.invalid", "Timestamps must be UTC.");
        }

        return new UserAccount(Guid.NewGuid(), email.Trim(), normalizedEmail, passwordHash,
            AccountRoles.Researcher, createdAtUtc);
    }
}
