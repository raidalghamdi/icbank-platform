using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for the deep-analysis standout quote.</summary>
public sealed class QuoteDto
{
    /// <summary>Gets or sets the quote text.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Gets or sets the quote's source.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>Gets or sets the quote's date.</summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }
}
