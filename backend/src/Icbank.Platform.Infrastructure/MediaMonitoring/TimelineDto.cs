using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for one <c>timeline</c> item.</summary>
public sealed class TimelineDto
{
    /// <summary>Gets or sets the event date.</summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>Gets or sets the event description.</summary>
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    /// <summary>Gets or sets the reporting outlet.</summary>
    [JsonPropertyName("outlet")]
    public string? Outlet { get; set; }

    /// <summary>Gets or sets the tone label.</summary>
    [JsonPropertyName("tone")]
    public string? Tone { get; set; }

    /// <summary>Gets or sets the mention count.</summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }
}
