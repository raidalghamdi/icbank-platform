namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>Typed shape for <c>final_media_reports.deep_analysis</c> (DATA-MODEL.md section 6, report section 6).</summary>
public sealed class DeepAnalysis
{
    /// <summary>Gets or sets the extracted keyword frequency list.</summary>
    public List<DeepAnalysisKeyword> Keywords { get; set; } = new();

    /// <summary>Gets or sets the optional standout quote.</summary>
    public DeepAnalysisQuote? Quote { get; set; }

    /// <summary>Gets or sets the identified strengths.</summary>
    public List<string> Strengths { get; set; } = new();

    /// <summary>Gets or sets the identified weaknesses.</summary>
    public List<string> Weaknesses { get; set; } = new();
}

/// <summary>One keyword entry nested under <see cref="DeepAnalysis"/>.</summary>
public sealed class DeepAnalysisKeyword
{
    /// <summary>Gets or sets the keyword text.</summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>Gets or sets the occurrence frequency.</summary>
    public int Frequency { get; set; }

    /// <summary>Gets or sets a usage-context snippet.</summary>
    public string Context { get; set; } = string.Empty;
}

/// <summary>Standout quote nested under <see cref="DeepAnalysis"/>.</summary>
public sealed class DeepAnalysisQuote
{
    /// <summary>Gets or sets the quote text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the quote's source.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the quote's date, as free text from the source payload.</summary>
    public string Date { get; set; } = string.Empty;
}
