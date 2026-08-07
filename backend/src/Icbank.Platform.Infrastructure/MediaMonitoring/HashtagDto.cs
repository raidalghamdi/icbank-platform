using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for one hashtag entry.</summary>
public sealed class HashtagDto
{
    /// <summary>Gets or sets the hashtag text.</summary>
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    /// <summary>Gets or sets the usage count.</summary>
    [JsonPropertyName("uses")]
    public int? Uses { get; set; }

    /// <summary>Gets or sets the trend descriptor.</summary>
    [JsonPropertyName("trend")]
    public string? Trend { get; set; }
}
