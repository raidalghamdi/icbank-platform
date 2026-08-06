using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for one alert entry.</summary>
public sealed class AlertDto
{
    /// <summary>Gets or sets the alert text.</summary>
    [JsonPropertyName("alert")]
    public string? Alert { get; set; }

    /// <summary>Gets or sets the suggested response/position.</summary>
    [JsonPropertyName("suggestedPosition")]
    public string? SuggestedPosition { get; set; }
}
