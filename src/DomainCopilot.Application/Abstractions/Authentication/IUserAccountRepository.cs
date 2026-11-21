using DomainCopilot.Domain.Users;

namespace DomainCopilot.Application.Abstractions.Authentication;

public interface IUserAccountRepository
{
    Task<UserAccount?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    void Add(UserAccount userAccount);
}
