namespace Icbank.Platform.Application.InternationalDays;

/// <summary>
/// Port for the per-IP AI-search rate limit (BUSINESS-RULES.md §4.1: 10 searches per IP per
/// rolling hour). The Node source used a single in-process <c>Map</c>, which resets on restart
/// and does not work correctly across multiple server instances (AMBIGUOUS-BR-5). This port
/// keeps the same 10-per-hour policy but abstracts the backing store so a distributed
/// implementation (Redis/SQL) can be swapped in without touching the handler -- the default
/// registration is an in-memory implementation matching the Node source's actual behavior,
/// flagged for product/infra sign-off on whether horizontal scaling requires the distributed
/// variant (see WAVE2-PORT-NOTES.md).
/// </summary>
public interface IInternationalDaySearchRateLimiter
{
    /// <summary>Attempts to consume one search slot for the given IP address.</summary>
    /// <param name="ipAddress">The caller's IP address.</param>
    /// <returns><c>true</c> if the caller is under the limit and the request may proceed; otherwise <c>false</c>.</returns>
    bool TryConsume(string ipAddress);

    /// <summary>Returns the number of remaining searches for the given IP address in the current window.</summary>
    /// <param name="ipAddress">The caller's IP address.</param>
    /// <returns>The remaining search count (0-10).</returns>
    int GetRemaining(string ipAddress);
}
