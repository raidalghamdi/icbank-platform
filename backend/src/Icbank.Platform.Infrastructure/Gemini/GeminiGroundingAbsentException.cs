namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// Thrown by the grounded (<c>google_search</c> tool-enabled) international-days search path when
/// Gemini answers without performing — or without reporting — any web search. Unlike Perplexity's
/// <c>sonar-pro</c> (always web-grounded), Gemini decides for itself whether to invoke
/// <c>google_search</c>; it can answer fluently from parametric memory and return
/// plausible-looking but entirely uncited/invented content. Treating that as a success would mean
/// silently fabricating "researched" Saudi-entity activations, which is worse than a visible
/// error — so the caller must treat this exception as a hard failure, not degrade to a
/// best-effort answer.
/// </summary>
public sealed class GeminiGroundingAbsentException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="GeminiGroundingAbsentException"/> class.</summary>
    public GeminiGroundingAbsentException()
        : base("Gemini response for a grounded search request carried no grounding metadata (no search-call queries, no url_citation annotations). Treating as failure rather than trusting unverified content.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GeminiGroundingAbsentException"/> class with a custom message.</summary>
    /// <param name="message">The exception message.</param>
    public GeminiGroundingAbsentException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GeminiGroundingAbsentException"/> class with a custom message and inner exception.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public GeminiGroundingAbsentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
