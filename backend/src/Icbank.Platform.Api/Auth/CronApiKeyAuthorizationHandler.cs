using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Icbank.Platform.Api.Auth;

/// <summary>
/// Validates the <c>X-Api-Key</c> header against the configuration-bound cron API key using a
/// constant-time comparison (closing SEC-19's timing-side-channel finding for the same class of
/// shared-secret check). If <see cref="CronApiKeyOptions.ApiKey"/> is unset, the requirement can
/// never succeed — there is no fallback literal anywhere in this class (closes SEC-13).
/// </summary>
public sealed class CronApiKeyAuthorizationHandler : AuthorizationHandler<CronApiKeyRequirement>
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CronApiKeyOptions _options;

    /// <summary>Initializes a new instance of the <see cref="CronApiKeyAuthorizationHandler"/> class.</summary>
    /// <param name="httpContextAccessor">Accessor for the current request's <see cref="HttpContext"/>.</param>
    /// <param name="options">The bound cron API key configuration.</param>
    public CronApiKeyAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IOptions<CronApiKeyOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CronApiKeyRequirement requirement)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
        {
            // Why: closes SEC-13 — an unset key means the requirement is rejected outright,
            // never silently satisfied by a hardcoded fallback.
            return Task.CompletedTask;
        }

        var presentedKey = _httpContextAccessor.HttpContext?.Request.Headers[ApiKeyHeaderName].ToString();
        if (!string.IsNullOrEmpty(presentedKey) && ConstantTimeEquals(presentedKey, _options.ApiKey))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool ConstantTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
