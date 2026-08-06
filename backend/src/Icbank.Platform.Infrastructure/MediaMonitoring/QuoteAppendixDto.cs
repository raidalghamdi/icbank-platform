using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for one quotes-appendix entry.</summary>
public sealed class QuoteAppendixDto
{
    /// <summary>Gets or sets the quote text.</summary>
    [JsonPropertyName("quote")]
    public string? Quote { get; set; }

    /// <summary>Gets or sets the quote's source.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>Gets or sets the quote's date.</summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>Gets or sets the related topic.</summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }
}
