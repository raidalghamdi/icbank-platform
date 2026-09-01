using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Gac;

namespace Icbank.Platform.Application.MediaMonitoring.Appearance;

/// <summary>
/// Loads the monitored press items and social posts of a report period and measures the appearance
/// analysis over them. Reports read their figures through this loader rather than from a stored
/// snapshot so that archived reports also show measured numbers instead of the zeros the
/// generation prompt legitimately produced when no engagement data was available.
/// </summary>
public static class MediaAppearanceLoader
{
    /// <summary>Measures the appearance analysis for one report period.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateFrom">The inclusive UTC start of the report period.</param>
    /// <param name="dateTo">The inclusive UTC end of the report period.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The measured analysis for the period.</returns>
    public static async Task<MediaAppearanceAnalysisDto> LoadAsync(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(queryExecutor);

        List<GacNewsItem> news = await queryExecutor.ToListAsync(
            dbContext.GacNewsItems.Where(n => n.PublishedAt >= dateFrom && n.PublishedAt <= dateTo), cancellationToken);
        List<GacSocialPost> posts = await queryExecutor.ToListAsync(
            dbContext.GacSocialPosts.Where(p => p.PostedAt >= dateFrom && p.PostedAt <= dateTo), cancellationToken);

        return MediaAppearanceAnalyzer.Analyze(news, posts);
    }
}
