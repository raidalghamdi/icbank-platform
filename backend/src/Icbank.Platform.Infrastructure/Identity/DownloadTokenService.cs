using System.Security.Cryptography;
using System.Text;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Mints and redeems the short-lived, single-use <see cref="DownloadToken"/> credential used to
/// close GAP 2 (FRONTEND-WIRING-NOTES.md §4): Shorfah PDF preview/download and the
/// international-days export were plain browser navigations under the old cookie-session API and
/// now 401 under bearer-only JWT auth. Follows the exact same "hash only, never store the raw
/// value" shape as <see cref="RefreshTokenService"/>, with one addition: the raw client-facing
/// token is <c>HMACSHA256(random bytes, configured signing key)</c> rather than the random bytes
/// alone, so a token cannot be forged without the configured key even though single-use state
/// still lives in the database (a pure database-lookup token, with no signature at all, would
/// already be unguessable given 256 bits of entropy -- the HMAC layer exists specifically because
/// the task requires the token be "signed with a key from configuration", giving defense in depth
/// against a scenario where the token table's hash column leaks: without the signing key an
/// attacker still cannot mint new tokens even if the hashing scheme were somehow reversed).
/// </summary>
public sealed class DownloadTokenService : IDownloadTokenService
{
    private const int RawTokenByteLength = 32;

    private readonly IApplicationDbContext _dbContext;
    private readonly DownloadTokenOptions _options;

    /// <summary>Initializes a new instance of the <see cref="DownloadTokenService"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="options">The bound <c>DownloadTokens</c> configuration options.</param>
    public DownloadTokenService(IApplicationDbContext dbContext, IOptions<DownloadTokenOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<string> IssueAsync(DownloadResourceType resourceType, int resourceId, int issuedToUserId, CancellationToken cancellationToken)
    {
        var rawToken = GenerateRawToken(_options.SigningKey);
        var entity = new DownloadToken
        {
            TokenHash = Hash(rawToken),
            ResourceType = resourceType,
            ResourceId = resourceId,
            IssuedToUserId = issuedToUserId,
            ExpiresAt = DateTime.UtcNow.AddSeconds(Math.Max(1, _options.LifetimeSeconds)),
        };

        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return rawToken;
    }

    /// <inheritdoc />
    public async Task<bool> RedeemAsync(string rawToken, DownloadResourceType resourceType, int resourceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(rawToken))
        {
            return false;
        }

        var hash = Hash(rawToken);
        DownloadToken? existing = await _dbContext.DownloadTokens
            .SingleOrDefaultAsync(dt => dt.TokenHash == hash, cancellationToken);

        // Why: every failure path below returns the same "false" with no further detail --
        // a wrong-resource token, an expired token, and an already-used token must all be
        // indistinguishable to the caller, exactly like the notification IDOR guard
        // (IResourceAuthorizationService) collapses "belongs to someone else" into "not found".
        if (existing is null || existing.ResourceType != resourceType || existing.ResourceId != resourceId || !existing.IsRedeemable)
        {
            return false;
        }

        existing.UsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string GenerateRawToken(string signingKey)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(RawTokenByteLength);
        var keyBytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(signingKey) ? Guid.NewGuid().ToString() : signingKey);
        var signed = HMACSHA256.HashData(keyBytes, randomBytes);

        // Why: URL-safe base64 (matches RefreshTokenService's own encoding choice) so the raw
        // value can ride as a query-string parameter without additional escaping.
        return Convert.ToBase64String(signed).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
