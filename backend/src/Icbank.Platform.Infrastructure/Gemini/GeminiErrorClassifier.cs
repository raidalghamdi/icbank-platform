namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// Ports <c>isTransientGeminiError</c>/<c>isModelLevelError</c> from <c>aiProviders.ts</c>
/// verbatim: both are case-insensitive substring scans over the error message, not structured
/// error codes. The exact substrings below are copied character-for-character from the Node
/// source so the retry/fallback behaviour matches precisely.
/// </summary>
public static class GeminiErrorClassifier
{
    // Why: kept as literal arrays (not a Regex) — the Node source used `.some(substr => msg.includes(substr))`,
    // and a Regex alternation would risk accidental regex-metacharacter behavior for strings like "429".
    private static readonly string[] TransientMarkers =
    {
        "503", "unavailable", "overloaded", "high demand", "429", "rate limit",
        "deadline", "timeout", "econnreset", "socket hang up", "fetch failed",
    };

    private static readonly string[] ModelLevelMarkers =
    {
        "404", "not_found", "no longer available", "is not found", "permission_denied", "unsupported",
    };

    /// <summary>Determines whether an error message indicates a transient condition worth retrying the same model.</summary>
    /// <param name="message">The raw error message (exception message or HTTP body).</param>
    /// <returns><c>true</c> when the message contains any transient marker.</returns>
    public static bool IsTransient(string? message) => ContainsAny(message, TransientMarkers);

    /// <summary>Determines whether an error message indicates the model itself is unusable, so the caller should skip straight to the next model without retrying.</summary>
    /// <param name="message">The raw error message (exception message or HTTP body).</param>
    /// <returns><c>true</c> when the message contains any model-level marker.</returns>
    public static bool IsModelLevel(string? message) => ContainsAny(message, ModelLevelMarkers);

    private static bool ContainsAny(string? message, string[] markers)
    {
        if (string.IsNullOrEmpty(message))
        {
            return false;
        }

        var lower = message.ToLowerInvariant();
        foreach (var marker in markers)
        {
            if (lower.Contains(marker, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
