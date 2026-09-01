namespace Icbank.Platform.Application.MediaMonitoring.Appearance;

/// <summary>
/// The measured media-appearance analysis for a report period. Every figure here is counted from
/// the monitored archive itself, never produced by the language model, so a report can be trusted
/// as a factual record of how often and where the authority actually appeared.
/// </summary>
/// <param name="TotalAppearances">All monitored appearances in the period, press plus social.</param>
/// <param name="PressAppearances">The monitored press/news items in the period.</param>
/// <param name="SocialAppearances">The monitored social posts in the period.</param>
/// <param name="DistinctOutlets">The number of distinct publishing outlets behind the press items.</param>
/// <param name="ActiveDays">The number of Riyadh-local days that carried at least one appearance.</param>
/// <param name="AveragePerDay">Appearances per active day, rounded to one decimal.</param>
/// <param name="PeakDay">The busiest Riyadh-local day in ISO <c>yyyy-MM-dd</c> form, or null when the period is empty.</param>
/// <param name="PeakDayAppearances">The appearance count on <paramref name="PeakDay"/>.</param>
/// <param name="TopOutlets">The most active outlets, ordered by appearance count.</param>
/// <param name="DailyTrend">The per-day appearance counts, ascending, covering only days that carried coverage.</param>
/// <param name="Platforms">The measured social platforms; empty when no social channel is connected.</param>
/// <param name="HasSocialData">Whether any social post was measured, so the UI can say so instead of printing zeros.</param>
public sealed record MediaAppearanceAnalysisDto(
    int TotalAppearances,
    int PressAppearances,
    int SocialAppearances,
    int DistinctOutlets,
    int ActiveDays,
    double AveragePerDay,
    string? PeakDay,
    int PeakDayAppearances,
    IReadOnlyList<MediaAppearanceOutletDto> TopOutlets,
    IReadOnlyList<MediaAppearanceDayDto> DailyTrend,
    IReadOnlyList<MediaAppearancePlatformDto> Platforms,
    bool HasSocialData)
{
    /// <summary>Gets the analysis shape used when a period carries no monitored coverage at all.</summary>
    public static MediaAppearanceAnalysisDto Empty { get; } = new(
        0, 0, 0, 0, 0, 0, null, 0, [], [], [], false);
}
