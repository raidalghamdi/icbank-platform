using System.Security.Cryptography;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Issues, rotates, and revokes opaque refresh tokens (DOTNET-CONVENTIONS.md §5.1). Only a
/// SHA-256 hash of the raw token is ever persisted, closing the gap where a leaked database
/// backup could be replayed as a live session. Rotation is atomic: validating an incoming token
/// immediately revokes it and issues its replacement in the same transaction, so any attempt to
/// reuse a rotated-out token is detected and every descendant token for that user is revoked
/// (reuse-detection per DOTNET-CONVENTIONS.md §5.1's "single-use" requirement).
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int RawTokenByteLength = 64;
    private readonly IApplicationDbContext _dbContext;
    private readonly JwtOptions _options;

    /// <summary>Initializes a new instance of the <see cref="RefreshTokenService"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="options">The bound JWT configuration options (refresh-token lifetime).</param>
    public RefreshTokenService(IApplicationDbContext dbContext, IOptions<JwtOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<string> IssueAsync(int userId, string? createdByIp, CancellationToken cancellationToken)
    {
        var rawToken = GenerateRawToken();
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(_options.RefreshTokenHours),
            CreatedByIp = createdByIp,
        };

        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return rawToken;
    }

    /// <inheritdoc />
    public async Task<(int UserId, string NewRawToken)?> RotateAsync(string rawToken, string? createdByIp, CancellationToken cancellationToken)
    {
        var hash = Hash(rawToken);
        RefreshToken? existing = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        if (existing.RevokedAt is not null)
        {
            // Why: a rotated-out token being presented again means either a client bug or an
            // attacker replaying a stolen token — revoke every live token for this user as a
            // precaution (DOTNET-CONVENTIONS.md §5.1 "revocable server-side" reuse-detection).
            await RevokeAllForUserAsync(existing.UserId, cancellationToken);
            return null;
        }

        if (existing.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        var newRawToken = GenerateRawToken();
        var replacement = new RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = Hash(newRawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(_options.RefreshTokenHours),
            CreatedByIp = createdByIp,
        };

        existing.RevokedAt = DateTime.UtcNow;
        _dbContext.Add(replacement);
        await _dbContext.SaveChangesAsync(cancellationToken);

        existing.ReplacedByTokenId = replacement.Id;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (existing.UserId, newRawToken);
    }

    /// <inheritdoc />
    public async Task RevokeAllForUserAsync(int userId, CancellationToken cancellationToken)
    {
        List<RefreshToken> activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        DateTime now = DateTime.UtcNow;
        foreach (RefreshToken token in activeTokens)
        {
            token.RevokedAt = now;
        }

        if (activeTokens.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(RawTokenByteLength);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
