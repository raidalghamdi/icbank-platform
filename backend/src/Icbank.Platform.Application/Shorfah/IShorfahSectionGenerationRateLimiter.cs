namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Port for rate-limiting <c>POST /shorfah/sections/:id/generate</c> (task requirement: "AI
/// generation ... is an external-cost abuse vector: authorize, rate limit via the existing
/// limiter, and audit"). Follows the exact same per-user/sliding-window shape as
/// <see cref="IShorfahSendInitialRateLimiter"/> and <c>IDesignGenerationRateLimiter</c>.
/// </summary>
public interface IShorfahSectionGenerationRateLimiter
{
    /// <summary>Attempts to consume one generation slot for the given admin user.</summary>
    /// <param name="userId">The authenticated caller's user id.</param>
    /// <returns><c>true</c> if the caller is under the limit and the request may proceed; otherwise <c>false</c>.</returns>
    bool TryConsume(int userId);
}
