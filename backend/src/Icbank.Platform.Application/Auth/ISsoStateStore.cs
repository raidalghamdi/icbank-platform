namespace Icbank.Platform.Application.Auth;

/// <summary>
/// Server-side store for in-flight PKCE state (BUSINESS-RULES.md §11.2). The old system used an
/// in-memory <c>Map</c> that "would fail" under more than one instance; this port is implemented
/// against <c>IDistributedCache</c> in Infrastructure so a multi-instance deployment works — see
/// AUTH-PORT-NOTES.md for the Redis-backend recommendation for production.
/// </summary>
public interface ISsoStateStore
{
    /// <summary>Persists a PKCE code verifier and the validated redirect target under an opaque state key, with a short TTL.</summary>
    /// <param name="state">The opaque, randomly generated state value.</param>
    /// <param name="codeVerifier">The PKCE code verifier to redeem on callback.</param>
    /// <param name="redirectTarget">The allow-listed post-login redirect target (closes SEC-11).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(string state, string codeVerifier, string redirectTarget, CancellationToken cancellationToken);

    /// <summary>Retrieves and immediately invalidates the state entry (single-use), or returns <c>null</c> if it doesn't exist or has expired.</summary>
    /// <param name="state">The opaque state value presented on callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted PKCE verifier and redirect target, or <c>null</c>.</returns>
    Task<(string CodeVerifier, string RedirectTarget)?> ConsumeAsync(string state, CancellationToken cancellationToken);
}
