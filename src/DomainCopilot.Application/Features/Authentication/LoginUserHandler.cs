using DomainCopilot.Application.Abstractions.Authentication;
using DomainCopilot.Domain.Users;

namespace DomainCopilot.Application.Features.Authentication;

public sealed class LoginUserHandler(
    IUserAccountRepository users,
    IPasswordCredentialService passwords,
    IAccessTokenIssuer tokenIssuer,
    TimeProvider timeProvider)
{
    public async Task<LoginUserResult?> Handle(LoginUserRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = UserAccount.NormalizeEmail(request.Email);
        var user = await users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !passwords.VerifyPassword(user.PasswordHash, request.Password))
        {
            return null;
        }

        var token = tokenIssuer.Issue(user, timeProvider.GetUtcNow());
        return new LoginUserResult(token.Token, "Bearer", token.ExpiresAtUtc);
    }
}
