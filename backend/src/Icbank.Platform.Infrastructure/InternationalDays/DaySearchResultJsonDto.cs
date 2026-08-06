using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.InternationalDays;

/// <summary>Wire shape for the international-day search prompt's JSON response (BUSINESS-RULES.md §4.2's exact snake_case keys). Nested shapes live in their own files (StyleCop SA1402).</summary>
public sealed class DaySearchResultJsonDto
{
    /// <summary>Gets or sets the Arabic day name.</summary>
    [JsonPropertyName("day_name_ar")]
    public string? DayNameAr { get; set; }

    /// <summary>Gets or sets the English day name.</summary>
    [JsonPropertyName("day_name_en")]
    public string? DayNameEn { get; set; }

    /// <summary>Gets or sets the free-text annual date.</summary>
    [JsonPropertyName("annual_date")]
    public string? AnnualDate { get; set; }

    /// <summary>Gets or sets the official sponsoring body.</summary>
    [JsonPropertyName("official_organizer")]
    public string? OfficialOrganizer { get; set; }

    /// <summary>Gets or sets the organizer claim's source URL.</summary>
    [JsonPropertyName("official_organizer_source")]
    public string? OfficialOrganizerSource { get; set; }

    /// <summary>Gets or sets the historical summary.</summary>
    [JsonPropertyName("history_summary")]
    public string? HistorySummary { get; set; }

    /// <summary>Gets or sets the history summary's source URL.</summary>
    [JsonPropertyName("history_source")]
    public string? HistorySource { get; set; }

    /// <summary>Gets or sets the current year's Arabic theme.</summary>
    [JsonPropertyName("current_theme_ar")]
    public string? CurrentThemeAr { get; set; }

    /// <summary>Gets or sets the current year's English theme.</summary>
    [JsonPropertyName("current_theme_en")]
    public string? CurrentThemeEn { get; set; }

    /// <summary>Gets or sets the theme's source URL.</summary>
    [JsonPropertyName("theme_source_url")]
    public string? ThemeSourceUrl { get; set; }

    /// <summary>Gets or sets the Saudi-entity activations.</summary>
    [JsonPropertyName("activations")]
    public List<DaySearchActivationJsonDto>? Activations { get; set; }

    /// <summary>Gets or sets the visual design samples.</summary>
    [JsonPropertyName("design_samples")]
    public List<DaySearchDesignSampleJsonDto>? DesignSamples { get; set; }

    /// <summary>Gets or sets the suggested activation ideas.</summary>
    [JsonPropertyName("suggestions")]
    public List<string>? Suggestions { get; set; }

    /// <summary>Gets or sets the cited sources.</summary>
    [JsonPropertyName("sources")]
    public List<DaySearchSourceJsonDto>? Sources { get; set; }
}
