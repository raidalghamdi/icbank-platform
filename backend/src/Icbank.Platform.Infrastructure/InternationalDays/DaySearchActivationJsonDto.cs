using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.InternationalDays;

/// <summary>Wire shape for one <c>activations</c> entry.</summary>
public sealed class DaySearchActivationJsonDto
{
    /// <summary>Gets or sets the Saudi entity name.</summary>
    [JsonPropertyName("entity_name")]
    public string? EntityName { get; set; }

    /// <summary>Gets or sets the entity type.</summary>
    [JsonPropertyName("entity_type")]
    public string? EntityType { get; set; }

    /// <summary>Gets or sets the activation type.</summary>
    [JsonPropertyName("activation_type")]
    public string? ActivationType { get; set; }

    /// <summary>Gets or sets the platform.</summary>
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    /// <summary>Gets or sets the description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Gets or sets the source URL.</summary>
    [JsonPropertyName("source_url")]
    public string? SourceUrl { get; set; }

    /// <summary>Gets or sets the country.</summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>Gets or sets the activation year.</summary>
    [JsonPropertyName("year")]
    public int? Year { get; set; }
}
