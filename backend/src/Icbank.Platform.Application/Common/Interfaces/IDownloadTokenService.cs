using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Port for minting and redeeming the short-lived, single-use <see cref="DownloadToken"/>
/// credential that lets a plain browser navigation reach a bearer-only download endpoint (GAP 2 --
/// FRONTEND-WIRING-NOTES.md §4). Minting must only ever be called after the caller's normal
/// <c>[Authorize]</c> policy and resource-authorization check have already passed for the exact
/// resource being scoped -- this port has no opinion on who may read a resource, it only issues
/// and checks the one-time ticket. Redemption never substitutes for that check: the caller of
/// <see cref="RedeemAsync"/> is expected to re-run its own resource-authorization check for the
/// resource id the token names before serving any content.
/// </summary>
public interface IDownloadTokenService
{
    /// <summary>Mints a new single-use token scoped to one resource.</summary>
    /// <param name="resourceType">The resource family the token may be redeemed against.</param>
    /// <param name="resourceId">The single resource id the token may be redeemed against.</param>
    /// <param name="issuedToUserId">The authenticated user the token was minted for (forensic audit only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw token value to hand to the client. Never persisted or logged in raw form.</returns>
    Task<string> IssueAsync(DownloadResourceType resourceType, int resourceId, int issuedToUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to redeem a raw token for a specific (<paramref name="resourceType"/>,
    /// <paramref name="resourceId"/>) pair. Atomically marks the token used on success, so a
    /// concurrent or later replay of the same raw value always fails from this point on.
    /// </summary>
    /// <param name="rawToken">The raw token value presented by the client.</param>
    /// <param name="resourceType">The resource family the caller expects the token to be scoped to.</param>
    /// <param name="resourceId">The resource id the caller expects the token to be scoped to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the token existed, matched this exact resource, was not expired, and had not already been used; otherwise <c>false</c>.</returns>
    Task<bool> RedeemAsync(string rawToken, DownloadResourceType resourceType, int resourceId, CancellationToken cancellationToken);
}
