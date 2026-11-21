using DomainCopilot.Application.Abstractions.Authentication;
using DomainCopilot.Domain.Users;
using DomainCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DomainCopilot.Infrastructure.Authentication;

public sealed class EfUserAccountRepository(AppDbContext dbContext) : IUserAccountRepository
{
    public Task<UserAccount?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.UserAccounts.SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    public void Add(UserAccount userAccount) => dbContext.UserAccounts.Add(userAccount);
}
