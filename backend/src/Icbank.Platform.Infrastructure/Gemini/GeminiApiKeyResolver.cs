using Microsoft.Extensions.Configuration;

namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// Resolves the Gemini API key using the Node source's exact fallback order
/// (<c>aiProviders.ts</c>: <c>process.env.GEMINI_API_KEY ?? process.env.GOOGLE_AI_API_KEY ??
/// process.env.AI_INTEGRATIONS_GEMINI_API_KEY ?? ""</c>). <see cref="IConfiguration"/> indexer
/// lookups already fall back to environment variables (and, in Azure, Key Vault via the
/// configured provider) so this class never reads <c>Environment.GetEnvironmentVariable</c>
/// directly — it stays provider-agnostic exactly like every other secret in this codebase.
/// </summary>
/// <remarks>
/// Why: the resolved key is deliberately never logged, echoed into an exception message, or
/// written to a file anywhere in this codebase — grep for <c>ApiKey</c> in this namespace to
/// confirm every call site treats the return value as write-only, passed straight into an HTTP
/// header.
/// </remarks>
public static class GeminiApiKeyResolver
{
    private const string PrimaryKeyName = "GEMINI_API_KEY";
    private const string SecondaryKeyName = "GOOGLE_AI_API_KEY";
    private const string TertiaryKeyName = "AI_INTEGRATIONS_GEMINI_API_KEY";

    /// <summary>Resolves the configured Gemini API key, or <c>null</c> if none of the three names are set.</summary>
    /// <param name="configuration">The application configuration (env vars, Key Vault, etc.).</param>
    /// <returns>The resolved key, or <c>null</c> when unconfigured.</returns>
    public static string? Resolve(IConfiguration configuration)
    {
        var key = configuration[PrimaryKeyName];
        if (!string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        key = configuration[SecondaryKeyName];
        if (!string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        key = configuration[TertiaryKeyName];
        return string.IsNullOrWhiteSpace(key) ? null : key;
    }
}
