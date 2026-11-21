namespace DomainCopilot.Application.Abstractions.Authentication;

public interface IPasswordCredentialService
{
    string HashPassword(string password);
    bool VerifyPassword(string passwordHash, string password);
}
