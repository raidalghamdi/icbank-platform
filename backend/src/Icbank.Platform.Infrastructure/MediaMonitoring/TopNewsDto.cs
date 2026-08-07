using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for one <c>topNews</c> item.</summary>
public sealed class TopNewsDto
{
    /// <summary>Gets or sets the item date.</summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>Gets or sets the tone label.</summary>
    [JsonPropertyName("tone")]
    public string? Tone { get; set; }

    /// <summary>Gets or sets the headline.</summary>
    [JsonPropertyName("headline")]
    public string? Headline { get; set; }

    /// <summary>Gets or sets the supporting detail lines.</summary>
    [JsonPropertyName("details")]
    public List<string>? Details { get; set; }

    /// <summary>Gets or sets the source outlet.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }
}
