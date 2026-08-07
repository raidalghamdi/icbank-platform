namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Port for rate-limiting <c>POST /shorfah/issues/:id/send-initial</c> (task requirement: "it
/// must be authorized, rate limited via the existing limiter, and audited, since it is a cost
/// -abuse vector" -- it fans out real email sends across every assignment in an issue). Follows
/// the exact same per-user/sliding-window shape as <c>IDesignGenerationRateLimiter</c> (wave 3b)
/// and <c>IInternationalDaySearchRateLimiter</c> (wave 2), the two existing limiter abstractions
/// this codebase already established.
/// </summary>
public interface IShorfahSendInitialRateLimiter
{
    /// <summary>Attempts to consume one send-initial slot for the given admin user.</summary>
    /// <param name="userId">The authenticated caller's user id.</param>
    /// <returns><c>true</c> if the caller is under the limit and the request may proceed; otherwise <c>false</c>.</returns>
    bool TryConsume(int userId);
}
