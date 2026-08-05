using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Icbank.Platform.Application.Auth;
using Microsoft.Extensions.Options;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Server-side Azure AD authorization-code + PKCE client (BUSINESS-RULES.md §11.2). The code
/// exchange happens entirely on the server via <see cref="HttpClient"/> — the resulting token is
/// parsed here and only the derived <see cref="AzureAdUserInfo"/> crosses back into the
/// Application layer, never the raw JWT (closes SEC-04/SEC-05: nothing from this class is ever
/// written into an HTML response).
/// </summary>
public sealed class AzureAdClient : IAzureAdClient
{
    private const string AuthorityBaseUrl = "https://login.microsoftonline.com";
    private const string CodeChallengeMethod = "S256";
    private const string ResponseType = "code";
    private const string Scope = "openid profile email User.Read";

    private readonly HttpClient _httpClient;
    private readonly AzureAdOptions _options;

    /// <summary>Initializes a new instance of the <see cref="AzureAdClient"/> class.</summary>
    /// <param name="httpClientFactory">Factory for the named <c>idp</c> resilient <see cref="HttpClient"/>.</param>
    /// <param name="options">The bound Azure AD configuration options.</param>
    public AzureAdClient(IHttpClientFactory httpClientFactory, IOptions<AzureAdOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("idp");
        _options = options.Value;
    }

    /// <inheritdoc />
    public string BuildAuthorizationUrl(string state, string codeChallenge)
    {
        System.Collections.Specialized.NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = _options.ClientId;
        query["response_type"] = ResponseType;
        query["redirect_uri"] = _options.RedirectUri;
        query["response_mode"] = "query";
        query["scope"] = Scope;
        query["state"] = state;
        query["code_challenge"] = codeChallenge;
        query["code_challenge_method"] = CodeChallengeMethod;

        var baseUrl = $"{AuthorityBaseUrl}/{_options.TenantId}/oauth2/v2.0/authorize";
        return $"{baseUrl}?{query}";
    }

    /// <inheritdoc />
    public async Task<AzureAdUserInfo> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken)
    {
        var tokenEndpoint = $"{AuthorityBaseUrl}/{_options.TenantId}/oauth2/v2.0/token";

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
            ["code_verifier"] = codeVerifier,
            ["scope"] = Scope,
        });

        using HttpResponseMessage response = await _httpClient.PostAsync(new Uri(tokenEndpoint), content, cancellationToken);
        response.EnsureSuccessStatusCode();

        AzureTokenResponse payload = await response.Content.ReadFromJsonAsync<AzureTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Azure AD token response could not be parsed.");

        return ParseIdToken(payload.IdToken);
    }

    private static AzureAdUserInfo ParseIdToken(string idToken)
    {
        var parts = idToken.Split('.');
        var payloadJson = Base64UrlDecode(parts[1]);
        using var document = JsonDocument.Parse(payloadJson);
        JsonElement root = document.RootElement;

        var objectId = root.GetProperty("oid").GetString() ?? string.Empty;
        var email = (root.TryGetProperty("email", out JsonElement emailElement) ? emailElement.GetString() : null)
            ?? root.GetProperty("preferred_username").GetString() ?? string.Empty;
        var name = root.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;

        return new AzureAdUserInfo(objectId, email, name);
    }

    private static string Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        var paddingNeeded = (4 - (padded.Length % 4)) % 4;
        padded += new string('=', paddingNeeded);
        var bytes = Convert.FromBase64String(padded);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    private sealed record AzureTokenResponse([property: JsonPropertyName("id_token")] string IdToken);
}
