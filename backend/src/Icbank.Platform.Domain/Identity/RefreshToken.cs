using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// A single-use, rotatable refresh token (closes SEC-05/AUTH-01/AUTH-02 and DOTNET-CONVENTIONS.md
/// §5.1: "single-use, rotate on every refresh, revocable server-side, maintain a
/// revocation/allow-list store keyed by token id"). The raw token value is never stored — only
/// its SHA-256 hash — so a leaked database backup cannot be replayed as a live session.
/// </summary>
public sealed class RefreshToken : AuditableEntity
{
    /// <summary>Gets or sets the owning user's id.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the user navigation property.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the SHA-256 hash (hex) of the raw refresh-token value.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp this token expires at.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp this token was revoked at, if any (rotation or explicit logout).</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Gets or sets the id of the token that replaced this one on rotation, if any.</summary>
    public int? ReplacedByTokenId { get; set; }

    /// <summary>Gets or sets the client IP address the token was issued to, for forensic audit.</summary>
    public string? CreatedByIp { get; set; }

    /// <summary>Gets a value indicating whether this token is currently usable (not expired, not revoked).</summary>
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
