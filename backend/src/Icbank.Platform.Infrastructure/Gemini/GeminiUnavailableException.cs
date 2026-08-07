namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// Thrown when every model in the fallback chain has been exhausted (all transient retries used
/// up, or a non-retryable/non-model-level error was hit). Carries the Node source's shared Arabic
/// fallback message verbatim (<c>aiJSONWithFallback</c>/<c>aiTextWithFallback</c>'s final error)
/// so callers surface the same staff-facing text the original product used.
/// </summary>
public sealed class GeminiUnavailableException : Exception
{
    /// <summary>The Node source's verbatim shared fallback error message (BUSINESS-RULES.md §13).</summary>
    public const string FallbackMessageAr = "تعذّر توليد المحتوى حالياً بسبب ضغط مؤقت على نماذج الذكاء الاصطناعي. يرجى المحاولة بعد دقيقتين.";

    /// <summary>Initializes a new instance of the <see cref="GeminiUnavailableException"/> class with the verbatim Arabic fallback message.</summary>
    public GeminiUnavailableException()
        : base(FallbackMessageAr)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GeminiUnavailableException"/> class with the verbatim Arabic fallback message and an inner exception.</summary>
    /// <param name="innerException">The last underlying failure from the model chain.</param>
    public GeminiUnavailableException(Exception innerException)
        : base(FallbackMessageAr, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GeminiUnavailableException"/> class with a custom message (for test/serialization support).</summary>
    /// <param name="message">The exception message.</param>
    public GeminiUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GeminiUnavailableException"/> class with a custom message and inner exception.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public GeminiUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
