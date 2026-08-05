namespace Icbank.Platform.Application.Designs;

/// <summary>
/// Port for rate-limiting external-cost design-generation calls (AI background generation,
/// icon-event AI extraction, headless-render image generation). The task brief flags these as an
/// "external-cost abuse vector" requiring reuse of "the existing rate-limiter abstraction added
/// in wave 2" -- this port follows the exact same per-key/sliding-window shape as
/// <c>IInternationalDaySearchRateLimiter</c>, keyed by authenticated user id rather than IP,
/// since every route this guards requires <c>[Authorize]</c> (unlike the anonymous
/// international-days search).
/// </summary>
public interface IDesignGenerationRateLimiter
{
    /// <summary>Attempts to consume one generation slot for the given user.</summary>
    /// <param name="userId">The authenticated caller's user id.</param>
    /// <returns><c>true</c> if the caller is under the limit and the request may proceed; otherwise <c>false</c>.</returns>
    bool TryConsume(int userId);
}
