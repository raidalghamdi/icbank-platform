namespace Icbank.Platform.Application.Auth;

/// <summary>
/// Port for the server-side half of the Azure AD authorization-code + PKCE flow
/// (BUSINESS-RULES.md §11.2). The Application layer never sees a raw token from Azure AD beyond
/// this exchange — the callback handler only receives the resulting <see cref="AzureAdUserInfo"/>.
/// </summary>
public interface IAzureAdClient
{
    /// <summary>Builds the Microsoft identity platform authorization URL to redirect the browser to, starting the PKCE flow.</summary>
    /// <param name="state">The opaque state value, persisted server-side and re-checked on callback.</param>
    /// <param name="codeChallenge">The PKCE code challenge (SHA-256 of the code verifier, base64url).</param>
    /// <returns>The absolute authorization URL.</returns>
    string BuildAuthorizationUrl(string state, string codeChallenge);

    /// <summary>Exchanges an authorization code (plus its PKCE verifier) for the caller's identity, entirely server-side.</summary>
    /// <param name="code">The authorization code returned on the callback.</param>
    /// <param name="codeVerifier">The PKCE code verifier matching the challenge sent to <see cref="BuildAuthorizationUrl"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticated Azure AD user's identity claims.</returns>
    Task<AzureAdUserInfo> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken);
}
