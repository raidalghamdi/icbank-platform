using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.InternationalDays;

/// <summary>Wire shape for one <c>sources</c> entry.</summary>
public sealed class DaySearchSourceJsonDto
{
    /// <summary>Gets or sets the source URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Gets or sets the source title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Gets or sets the publisher name.</summary>
    [JsonPropertyName("publisher")]
    public string? Publisher { get; set; }
}
