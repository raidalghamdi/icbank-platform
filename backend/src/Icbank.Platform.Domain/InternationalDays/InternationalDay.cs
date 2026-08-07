using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.InternationalDays;

/// <summary>
/// Catalogue of UN/international observance days tracked for campaign planning
/// (DATA-MODEL.md section 3.6 <c>international_days</c>).
/// </summary>
public sealed class InternationalDay : AuditableEntity
{
    /// <summary>Gets or sets the Arabic day name.</summary>
    public string DayNameAr { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional English day name.</summary>
    public string? DayNameEn { get; set; }

    /// <summary>
    /// Gets or sets the free-text annual date (e.g. "21 مارس" or "MM-DD"), parsed by a regex in
    /// the source application rather than stored as a real date -- preserved as-is (fragile
    /// format, not tightened during this port; see DOMAIN-PORT-NOTES.md).
    /// </summary>
    public string? AnnualDate { get; set; }

    /// <summary>Gets or sets the optional category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the official organizing body, if known.</summary>
    public string? OfficialOrganizer { get; set; }

    /// <summary>Gets or sets the source URL for the official organizer claim.</summary>
    public string? OfficialOrganizerSource { get; set; }

    /// <summary>Gets or sets a summary of the day's history.</summary>
    public string? HistorySummary { get; set; }

    /// <summary>Gets or sets the source URL for the history summary.</summary>
    public string? HistorySource { get; set; }

    /// <summary>Gets or sets AI-generated activation suggestions.</summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>Gets or sets the UTC timestamp of the last AI search, used for a 7-day cache window.</summary>
    public DateTimeOffset? LastSearchedAt { get; set; }

    /// <summary>Gets the per-year themes recorded for this day.</summary>
    public ICollection<DayYearlyTheme> YearlyThemes { get; init; } = new List<DayYearlyTheme>();

    /// <summary>Gets the campaign activations recorded for this day.</summary>
    public ICollection<DayActivation> Activations { get; init; } = new List<DayActivation>();
}
