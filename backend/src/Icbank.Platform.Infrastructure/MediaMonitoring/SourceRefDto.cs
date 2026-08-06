using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for one source-list entry.</summary>
public sealed class SourceRefDto
{
    /// <summary>Gets or sets the source name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Gets or sets the source URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Gets or sets an optional description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
