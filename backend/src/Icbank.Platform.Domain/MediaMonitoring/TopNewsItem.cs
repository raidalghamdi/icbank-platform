namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>One entry of <c>final_media_reports.top_news</c> (DATA-MODEL.md section 6, report section 2).</summary>
public sealed class TopNewsItem
{
    /// <summary>Gets or sets the news item's date, as free text from the source payload.</summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>Gets or sets the editorial tone label.</summary>
    public string Tone { get; set; } = string.Empty;

    /// <summary>Gets or sets the headline.</summary>
    public string Headline { get; set; } = string.Empty;

    /// <summary>Gets or sets the supporting detail lines.</summary>
    public List<string> Details { get; set; } = new();

    /// <summary>Gets or sets the source outlet.</summary>
    public string Source { get; set; } = string.Empty;
}
