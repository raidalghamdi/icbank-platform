using Microsoft.Extensions.Configuration;

namespace Icbank.Platform.Infrastructure.News;

/// <summary>
/// Resolves the NewsData.io API key from configuration, following the same
/// read-it-directly-and-never-bind-it approach as <see cref="Gemini.GeminiApiKeyResolver"/> so the
/// secret never lands on an options object that could be logged or serialized.
/// </summary>
public static class NewsDataApiKeyResolver
{
    /// <summary>The accepted configuration keys, in precedence order.</summary>
    private static readonly string[] KeyNames = { "NEWSDATA_API_KEY", "NEWS_API_KEY" };

    /// <summary>Reads the first configured key.</summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The key, or null when none of the accepted names is set.</returns>
    public static string? Resolve(IConfiguration configuration)
    {
        foreach (var name in KeyNames)
        {
            var value = configuration[name];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
