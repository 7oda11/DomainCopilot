using DomainCopilot.Application.Abstractions.Authentication;
using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Domain.Users;

namespace DomainCopilot.Application.Features.Authentication;

public sealed class RegisterUserHandler(
    IUserAccountRepository users,
    IPasswordCredentialService passwords,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RegisterUserResult> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = UserAccount.NormalizeEmail(request.Email);
        if (await users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            throw new EmailAlreadyRegisteredException();
        }

        var user = UserAccount.CreateResearcher(request.Email, passwords.HashPassword(request.Password),
            timeProvider.GetUtcNow());
        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterUserResult(user.Id, user.Email, user.Role);
    }
}
