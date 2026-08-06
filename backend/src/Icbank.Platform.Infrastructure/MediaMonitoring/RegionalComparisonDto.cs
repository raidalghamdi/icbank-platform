using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for one regional-comparison row.</summary>
public sealed class RegionalComparisonDto
{
    /// <summary>Gets or sets the compared authority's name.</summary>
    [JsonPropertyName("authority")]
    public string? Authority { get; set; }

    /// <summary>Gets or sets the authority's country.</summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>Gets or sets the mention count.</summary>
    [JsonPropertyName("mentions")]
    public int? Mentions { get; set; }

    /// <summary>Gets or sets the tone label.</summary>
    [JsonPropertyName("tone")]
    public string? Tone { get; set; }

    /// <summary>Gets or sets notable highlights.</summary>
    [JsonPropertyName("highlights")]
    public string? Highlights { get; set; }
}
