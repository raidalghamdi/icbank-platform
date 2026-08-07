using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for one deep-analysis keyword entry.</summary>
public sealed class KeywordDto
{
    /// <summary>Gets or sets the keyword text.</summary>
    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    /// <summary>Gets or sets the occurrence frequency.</summary>
    [JsonPropertyName("frequency")]
    public int? Frequency { get; set; }

    /// <summary>Gets or sets the usage-context snippet.</summary>
    [JsonPropertyName("context")]
    public string? Context { get; set; }
}
