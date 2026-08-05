using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Issues short-lived JWT access tokens signed with a configuration-bound symmetric key
/// (DOTNET-CONVENTIONS.md §5.1/§5.4). The effective permission set is embedded as claims so the
/// <c>PermissionAuthorizationHandler</c> never has to hit the database on every authorized
/// request — claims are the source of truth for the token's lifetime, re-resolved fresh on every
/// login/refresh.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private const string PermissionClaimType = "permission";
    private const string SuperAdminClaimType = "is_super_admin";
    private readonly JwtOptions _options;

    /// <summary>Initializes a new instance of the <see cref="JwtTokenService"/> class.</summary>
    /// <param name="options">The bound JWT configuration options.</param>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public AccessTokenResult IssueAccessToken(User user, IReadOnlyCollection<string> roleNames, IReadOnlyCollection<string> permissions, bool isSuperAdmin)
    {
        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        List<Claim> claims = new()
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(SuperAdminClaimType, isSuperAdmin ? bool.TrueString : bool.FalseString),
        };

        claims.AddRange(roleNames.Select(roleName => new Claim(ClaimTypes.Role, roleName)));
        claims.AddRange(permissions.Select(permission => new Claim(PermissionClaimType, permission)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokenResult(encoded, expiresAtUtc);
    }
}
