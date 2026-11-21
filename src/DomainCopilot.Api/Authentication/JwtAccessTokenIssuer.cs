using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DomainCopilot.Application.Abstractions.Authentication;
using DomainCopilot.Domain.Users;
using Microsoft.IdentityModel.Tokens;

namespace DomainCopilot.Api.Authentication;

public sealed class JwtAccessTokenIssuer(IConfiguration configuration) : IAccessTokenIssuer
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public IssuedAccessToken Issue(UserAccount userAccount, DateTimeOffset nowUtc)
    {
        var issuer = configuration["Authentication:Issuer"]!;
        var audience = configuration["Authentication:Audience"]!;
        var signingKey = configuration["Authentication:SigningKey"]!;
        var expiresAtUtc = nowUtc.Add(TokenLifetime);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userAccount.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, userAccount.Email),
            new("role", userAccount.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(issuer, audience, claims, nowUtc.UtcDateTime,
            expiresAtUtc.UtcDateTime, credentials);

        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(jwt), expiresAtUtc);
    }
}
