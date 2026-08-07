using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for the <c>editorialTone</c> section.</summary>
public sealed class EditorialToneDto
{
    /// <summary>Gets or sets the tone-distribution buckets.</summary>
    [JsonPropertyName("distribution")]
    public List<ToneBucketDto>? Distribution { get; set; }

    /// <summary>Gets or sets the topic-classification buckets.</summary>
    [JsonPropertyName("classification")]
    public List<ToneBucketDto>? Classification { get; set; }

    /// <summary>Gets or sets the source-type buckets.</summary>
    [JsonPropertyName("sources")]
    public List<ToneBucketDto>? Sources { get; set; }
}
