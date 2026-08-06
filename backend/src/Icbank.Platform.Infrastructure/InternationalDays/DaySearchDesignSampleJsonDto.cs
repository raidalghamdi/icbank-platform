using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.InternationalDays;

/// <summary>Wire shape for one <c>design_samples</c> entry.</summary>
public sealed class DaySearchDesignSampleJsonDto
{
    /// <summary>Gets or sets the publishing entity name.</summary>
    [JsonPropertyName("entity_name")]
    public string? EntityName { get; set; }

    /// <summary>Gets or sets the entity type.</summary>
    [JsonPropertyName("entity_type")]
    public string? EntityType { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    /// <summary>Gets or sets the description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Gets or sets the page URL.</summary>
    [JsonPropertyName("page_url")]
    public string? PageUrl { get; set; }

    /// <summary>Gets or sets the direct image URL.</summary>
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the country.</summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>Gets or sets the design's year.</summary>
    [JsonPropertyName("year")]
    public int? Year { get; set; }
}
