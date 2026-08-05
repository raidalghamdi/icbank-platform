namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>One entry of <c>final_media_reports.regional_comparison</c> (DATA-MODEL.md section 6, report section 7).</summary>
public sealed class RegionalComparison
{
    /// <summary>Gets or sets the compared authority's name.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Gets or sets the authority's country.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Gets or sets the mention count.</summary>
    public int Mentions { get; set; }

    /// <summary>Gets or sets the editorial tone label.</summary>
    public string Tone { get; set; } = string.Empty;

    /// <summary>Gets or sets notable highlights.</summary>
    public string Highlights { get; set; } = string.Empty;
}
