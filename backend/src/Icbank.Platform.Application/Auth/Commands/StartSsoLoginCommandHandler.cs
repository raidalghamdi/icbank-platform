using System.Security.Cryptography;
using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>Handles <see cref="StartSsoLoginCommand"/> — generates the PKCE pair, persists state server-side, and returns the Azure AD authorization URL to redirect to.</summary>
public sealed class StartSsoLoginCommandHandler : IRequestHandler<StartSsoLoginCommand, Result<string>>
{
    private const int StateByteLength = 16;
    private const int CodeVerifierByteLength = 32;

    private readonly IAzureAdClient _azureAdClient;
    private readonly ISsoStateStore _stateStore;
    private readonly ISsoOptionsProvider _options;

    /// <summary>Initializes a new instance of the <see cref="StartSsoLoginCommandHandler"/> class.</summary>
    /// <param name="azureAdClient">The Azure AD PKCE client port.</param>
    /// <param name="stateStore">The server-side PKCE state store.</param>
    /// <param name="options">The Azure AD SSO configuration.</param>
    public StartSsoLoginCommandHandler(IAzureAdClient azureAdClient, ISsoStateStore stateStore, ISsoOptionsProvider options)
    {
        _azureAdClient = azureAdClient;
        _stateStore = stateStore;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<Result<string>> Handle(StartSsoLoginCommand request, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return Result<string>.Failure("sso_disabled");
        }

        // Why: SEC-11 — the redirect target is validated against the allow-list BEFORE it is
        // ever persisted or acted on; an invalid target is rejected here, not "sanitized" later.
        var redirectTarget = RedirectTargetValidator.Validate(request.RequestedRedirect, _options.AllowedRedirectTargets);

        var state = GenerateUrlSafeToken(StateByteLength);
        var codeVerifier = GenerateUrlSafeToken(CodeVerifierByteLength);
        var codeChallenge = ComputeCodeChallenge(codeVerifier);

        await _stateStore.SaveAsync(state, codeVerifier, redirectTarget, cancellationToken);

        var authorizationUrl = _azureAdClient.BuildAuthorizationUrl(state, codeChallenge);
        return Result<string>.Success(authorizationUrl);
    }

    private static string GenerateUrlSafeToken(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
