namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Port for generating an audience-tiered media-monitoring report body (BUSINESS-RULES.md
/// §5.1's verbatim audience-tiered prompts). The Node source made three separate Gemini calls
/// (body, 2-3 line executive summary, 2-word tone classification); this port keeps the model
/// call out of Application (R-BE-002) and swappable in Infrastructure. Wave 3a ships a
/// deterministic, non-AI default implementation -- wiring a real LLM call is deferred, see
/// WAVE3A-PORT-NOTES.md.
/// </summary>
public interface IMediaReportNarrativeGenerator
{
    /// <summary>Generates the report body, executive summary, and overall tone for a formatted source-data block.</summary>
    /// <param name="audience">The audience tier key (<c>executive</c>, <c>analyst</c>, or <c>manager</c>).</param>
    /// <param name="formattedFeed">The flat numbered text block of source posts/news items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated narrative bundle.</returns>
    Task<MediaReportNarrative> GenerateAsync(string audience, string formattedFeed, CancellationToken cancellationToken);
}

/// <summary>The three-part output of <see cref="IMediaReportNarrativeGenerator"/> (BUSINESS-RULES.md §5.1).</summary>
/// <param name="ContentMd">The full Markdown report body.</param>
/// <param name="ExecutiveSummary">The 2-3 line executive summary.</param>
/// <param name="OverallTone">The 2-word overall tone classification.</param>
public sealed record MediaReportNarrative(string ContentMd, string ExecutiveSummary, string OverallTone);
