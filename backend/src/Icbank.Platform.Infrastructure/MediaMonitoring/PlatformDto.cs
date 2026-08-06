using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for one digital-presence platform entry.</summary>
public sealed class PlatformDto
{
    /// <summary>Gets or sets the platform name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Gets or sets the mention count.</summary>
    [JsonPropertyName("mentions")]
    public int? Mentions { get; set; }

    /// <summary>Gets or sets the repost count.</summary>
    [JsonPropertyName("reposts")]
    public int? Reposts { get; set; }

    /// <summary>Gets or sets the engagement count.</summary>
    [JsonPropertyName("engagement")]
    public int? Engagement { get; set; }

    /// <summary>Gets or sets the free-text reach figure.</summary>
    [JsonPropertyName("reach")]
    public string? Reach { get; set; }
}
