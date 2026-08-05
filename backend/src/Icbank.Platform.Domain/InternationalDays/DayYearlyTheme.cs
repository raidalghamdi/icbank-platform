using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.InternationalDays;

/// <summary>
/// Per-year campaign theme/slogan for a given day (DATA-MODEL.md section 3.6 <c>day_yearly_themes</c>).
/// </summary>
public sealed class DayYearlyTheme : AuditableEntity
{
    /// <summary>Gets or sets the owning day's id.</summary>
    public int DayId { get; set; }

    /// <summary>Gets or sets the day navigation property.</summary>
    public InternationalDay Day { get; set; } = null!;

    /// <summary>Gets or sets the campaign year.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the optional Arabic theme text.</summary>
    public string? ThemeAr { get; set; }

    /// <summary>Gets or sets the optional English theme text.</summary>
    public string? ThemeEn { get; set; }

    /// <summary>Gets or sets the source URL for the theme, if any.</summary>
    public string? ThemeSourceUrl { get; set; }
}
