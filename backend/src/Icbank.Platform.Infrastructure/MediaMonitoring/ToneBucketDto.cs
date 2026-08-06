using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for one unified tone/classification/source bucket.</summary>
public sealed class ToneBucketDto
{
    /// <summary>Gets or sets the bucket label.</summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>Gets or sets the percentage share.</summary>
    [JsonPropertyName("percent")]
    public double? Percent { get; set; }

    /// <summary>Gets or sets the raw count.</summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }
}
