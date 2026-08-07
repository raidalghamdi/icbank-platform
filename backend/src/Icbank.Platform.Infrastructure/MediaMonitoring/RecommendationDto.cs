using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for one recommendation entry.</summary>
public sealed class RecommendationDto
{
    /// <summary>Gets or sets the recommendation title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Gets or sets the recommendation description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Gets or sets the priority label.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Gets or sets the responsible party.</summary>
    [JsonPropertyName("responsible")]
    public string? Responsible { get; set; }

    /// <summary>Gets or sets the tracked KPI.</summary>
    [JsonPropertyName("kpi")]
    public string? Kpi { get; set; }

    /// <summary>Gets or sets the deadline.</summary>
    [JsonPropertyName("deadline")]
    public string? Deadline { get; set; }

    /// <summary>Gets or sets dependency notes.</summary>
    [JsonPropertyName("dependencies")]
    public string? Dependencies { get; set; }
}
