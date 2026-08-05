namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>One entry of <c>final_media_reports.quotes_appendix</c> (DATA-MODEL.md section 6).</summary>
public sealed class QuoteAppendixItem
{
    /// <summary>Gets or sets the quote text.</summary>
    public string Quote { get; set; } = string.Empty;

    /// <summary>Gets or sets the quote's source.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the quote's date, as free text from the source payload.</summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>Gets or sets the related topic.</summary>
    public string Topic { get; set; } = string.Empty;
}
