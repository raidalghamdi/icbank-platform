using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for the <c>digitalPresence</c> section.</summary>
public sealed class DigitalPresenceDto
{
    /// <summary>Gets or sets the per-platform breakdown.</summary>
    [JsonPropertyName("platforms")]
    public List<PlatformDto>? Platforms { get; set; }

    /// <summary>Gets or sets the trending hashtags.</summary>
    [JsonPropertyName("hashtags")]
    public List<HashtagDto>? Hashtags { get; set; }
}
