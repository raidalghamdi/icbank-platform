using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// A short-lived, single-use credential that lets a plain browser navigation (an
/// <c>&lt;a href&gt;</c> or <c>window.open</c>) reach a bearer-only, <c>[Authorize]</c>-protected
/// download endpoint that cannot attach an <c>Authorization</c> header (GAP 2 --
/// FRONTEND-WIRING-NOTES.md §4: Shorfah PDF preview/download and the international-days export).
///
/// Deliberately modeled the same way as <see cref="RefreshToken"/> rather than as a stateless
/// signed blob: single-use enforcement needs a persisted "already redeemed" fact, so a purely
/// stateless HMAC token would still need a replay-cache to be genuinely single-use, at which
/// point it is simpler and no less secure to just persist the token itself. Only the SHA-256 hash
/// of the raw token is stored -- never the raw value -- so a leaked database backup cannot be
/// replayed as a live download credential. The token is scoped to exactly one
/// (<see cref="ResourceType"/>, <see cref="ResourceId"/>) pair chosen at mint time by the same
/// authenticated request that already passed the endpoint's normal <c>[Authorize]</c> policy and
/// resource-existence check (<c>IResourceAuthorizationService</c>) -- redemption re-checks
/// resource authorization independently and never trusts the token alone to prove the bearer may
/// read that resource, only that a moment ago an authorized caller minted it for exactly this id.
/// </summary>
public sealed class DownloadToken : AuditableEntity
{
    /// <summary>Gets or sets the SHA-256 hash (hex) of the raw token value handed to the client.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Gets or sets which resource family this token is scoped to.</summary>
    public DownloadResourceType ResourceType { get; set; }

    /// <summary>Gets or sets the single resource id this token may be redeemed against.</summary>
    public int ResourceId { get; set; }

    /// <summary>Gets or sets the id of the user the token was minted for (for forensic audit only -- redemption does not require the same bearer).</summary>
    public int IssuedToUserId { get; set; }

    /// <summary>Gets or sets the UTC timestamp this token expires at. Kept short (configuration-driven, default 2 minutes).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp this token was redeemed at, if any. A non-null value makes every subsequent redemption attempt fail closed.</summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>Gets a value indicating whether this token can still be redeemed (not expired, not already used).</summary>
    public bool IsRedeemable => UsedAt is null && ExpiresAt > DateTime.UtcNow;
}
