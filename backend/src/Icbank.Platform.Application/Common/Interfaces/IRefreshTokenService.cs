namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Port for issuing, rotating, and revoking opaque refresh tokens (DOTNET-CONVENTIONS.md §5.1:
/// "single-use, rotate on every refresh, revocable server-side ... checked on every refresh").
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>Issues and persists a new refresh token for the given user, returning the raw (unhashed) value to set in the cookie.</summary>
    /// <param name="userId">The owning user's id.</param>
    /// <param name="createdByIp">The caller's IP address, for forensic audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw refresh-token value. Only its hash is persisted.</returns>
    Task<string> IssueAsync(int userId, string? createdByIp, CancellationToken cancellationToken);

    /// <summary>
    /// Validates a raw refresh-token value, and if valid, atomically revokes it and issues its
    /// replacement (rotation) — closing the single-use/rotation requirement. Returns <c>null</c>
    /// if the token is missing, expired, or already revoked (reuse detection).
    /// </summary>
    /// <param name="rawToken">The raw refresh-token value read from the cookie.</param>
    /// <param name="createdByIp">The caller's IP address, for forensic audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The owning user id and the new raw refresh-token value, or <c>null</c> if invalid.</returns>
    Task<(int UserId, string NewRawToken)?> RotateAsync(string rawToken, string? createdByIp, CancellationToken cancellationToken);

    /// <summary>Revokes every active refresh token for a user (used on logout and forced password resets).</summary>
    /// <param name="userId">The user whose tokens should be revoked.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAllForUserAsync(int userId, CancellationToken cancellationToken);
}
