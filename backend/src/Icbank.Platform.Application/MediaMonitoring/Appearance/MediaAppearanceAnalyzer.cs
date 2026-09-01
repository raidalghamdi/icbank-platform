using System.Globalization;
using Icbank.Platform.Domain.Gac;

namespace Icbank.Platform.Application.MediaMonitoring.Appearance;

/// <summary>
/// Counts the media-appearance figures of a report period straight from the monitored archive.
/// The 8-section prompt forbids the model from inventing statistics, so it correctly returned
/// zeros for every reach/engagement field whenever no social listening feed was attached — which
/// surfaced to readers as an "analysis" made entirely of zeros. Measuring here instead keeps the
/// section factual: press appearances, outlets and daily rhythm come from rows we actually hold,
/// and social platforms are reported only when posts for them exist.
/// </summary>
public static class MediaAppearanceAnalyzer
{
    private const int TopOutletLimit = 10;

    private const string UnknownOutlet = "مصدر غير محدد";

    /// <summary>Riyadh never observes daylight saving, so a fixed offset keeps day bucketing reproducible.</summary>
    private static readonly TimeSpan RiyadhOffset = TimeSpan.FromHours(3);

    /// <summary>Measures the appearance analysis for one report period.</summary>
    /// <param name="news">The monitored press items inside the period.</param>
    /// <param name="posts">The monitored social posts inside the period.</param>
    /// <returns>The measured analysis, or <see cref="MediaAppearanceAnalysisDto.Empty"/> when nothing was monitored.</returns>
    public static MediaAppearanceAnalysisDto Analyze(IReadOnlyList<GacNewsItem> news, IReadOnlyList<GacSocialPost> posts)
    {
        ArgumentNullException.ThrowIfNull(news);
        ArgumentNullException.ThrowIfNull(posts);

        if (news.Count == 0 && posts.Count == 0)
        {
            return MediaAppearanceAnalysisDto.Empty;
        }

        var total = news.Count + posts.Count;
        List<MediaAppearanceOutletDto> outlets = BuildOutlets(news, total);
        List<MediaAppearanceDayDto> trend = BuildDailyTrend(news, posts);
        List<MediaAppearancePlatformDto> platforms = BuildPlatforms(posts);
        MediaAppearanceDayDto? peak = trend.Count == 0 ? null : trend.MaxBy(d => d.Appearances);
        var activeDays = trend.Count;

        return new MediaAppearanceAnalysisDto(
            total,
            news.Count,
            posts.Count,
            outlets.Count,
            activeDays,
            activeDays == 0 ? 0 : Math.Round((double)total / activeDays, 1, MidpointRounding.AwayFromZero),
            peak?.Date,
            peak?.Appearances ?? 0,
            outlets.Take(TopOutletLimit).ToList(),
            trend,
            platforms,
            posts.Count > 0);
    }

    /// <summary>Resolves the outlet an item should be attributed to.</summary>
    /// <param name="item">The monitored press item.</param>
    /// <returns>The stored outlet name, the source host as a fallback, or an explicit unknown label.</returns>
    private static string ResolveOutlet(GacNewsItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.ExternalRef))
        {
            return item.ExternalRef.Trim();
        }

        if (Uri.TryCreate(item.SourceUrl, UriKind.Absolute, out Uri? uri))
        {
            var host = uri.Host;
            return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
        }

        return UnknownOutlet;
    }

    private static List<MediaAppearanceOutletDto> BuildOutlets(IReadOnlyList<GacNewsItem> news, int total) =>
        news.GroupBy(ResolveOutlet, StringComparer.Ordinal)
            .Select(g => new MediaAppearanceOutletDto(g.Key, g.Count(), Percent(g.Count(), total)))
            .OrderByDescending(o => o.Appearances)
            .ThenBy(o => o.Name, StringComparer.Ordinal)
            .ToList();

    private static List<MediaAppearanceDayDto> BuildDailyTrend(IReadOnlyList<GacNewsItem> news, IReadOnlyList<GacSocialPost> posts)
    {
        IEnumerable<DateTimeOffset> stamps = news
            .Select(n => n.PublishedAt ?? AsUtc(n.CreatedAt))
            .Concat(posts.Select(p => p.PostedAt ?? AsUtc(p.CreatedAt)));

        return stamps
            .GroupBy(RiyadhDay, StringComparer.Ordinal)
            .Select(g => new MediaAppearanceDayDto(g.Key, g.Count()))
            .OrderBy(d => d.Date, StringComparer.Ordinal)
            .ToList();
    }

    private static List<MediaAppearancePlatformDto> BuildPlatforms(IReadOnlyList<GacSocialPost> posts) =>
        posts.GroupBy(p => p.Platform)
            .Select(g => new MediaAppearancePlatformDto(
                PlatformLabel(g.Key),
                g.Count(),
                g.Sum(p => (p.Metrics?.Likes ?? 0) + (p.Metrics?.Comments ?? 0) + (p.Metrics?.Shares ?? 0)),
                g.Sum(p => p.Metrics?.Shares ?? 0)))
            .OrderByDescending(p => p.Posts)
            .ThenBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Audit stamps are stored as UTC <see cref="DateTime"/> values, so they are pinned to UTC
    /// explicitly instead of letting the implicit conversion read the host machine's zone.
    /// </summary>
    /// <param name="stamp">The stored audit timestamp.</param>
    /// <returns>The same instant as a UTC offset value.</returns>
    private static DateTimeOffset AsUtc(DateTime stamp) =>
        new(DateTime.SpecifyKind(stamp, DateTimeKind.Utc));

    private static string RiyadhDay(DateTimeOffset stamp) =>
        stamp.ToOffset(RiyadhOffset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static int Percent(int part, int total) =>
        total == 0 ? 0 : (int)Math.Round(part * 100.0 / total, MidpointRounding.AwayFromZero);

    private static string PlatformLabel(GacSocialPlatform platform) => platform switch
    {
        GacSocialPlatform.LinkedIn => "لينكدإن",
        GacSocialPlatform.Twitter => "إكس",
        GacSocialPlatform.Instagram => "إنستغرام",
        GacSocialPlatform.YouTube => "يوتيوب",
        _ => platform.ToString(),
    };
}
