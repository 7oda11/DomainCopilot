using DomainCopilot.Domain.Users;

namespace DomainCopilot.Application.Abstractions.Authentication;

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(UserAccount userAccount, DateTimeOffset nowUtc);
}

public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAtUtc);
