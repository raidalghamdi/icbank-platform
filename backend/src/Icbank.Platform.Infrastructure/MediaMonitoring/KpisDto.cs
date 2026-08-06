using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for the <c>kpis</c> object.</summary>
public sealed class KpisDto
{
    /// <summary>Gets or sets the total news count.</summary>
    [JsonPropertyName("totalNews")]
    public int? TotalNews { get; set; }

    /// <summary>Gets or sets the positive-sentiment percent.</summary>
    [JsonPropertyName("positivePercent")]
    public int? PositivePercent { get; set; }

    /// <summary>Gets or sets the media outlet count.</summary>
    [JsonPropertyName("mediaOutlets")]
    public int? MediaOutlets { get; set; }

    /// <summary>Gets or sets the key-topic count.</summary>
    [JsonPropertyName("keyTopics")]
    public int? KeyTopics { get; set; }

    /// <summary>Gets or sets the free-text reach figure.</summary>
    [JsonPropertyName("reach")]
    public string? Reach { get; set; }

    /// <summary>Gets or sets the alert count.</summary>
    [JsonPropertyName("alertsCount")]
    public int? AlertsCount { get; set; }
}
