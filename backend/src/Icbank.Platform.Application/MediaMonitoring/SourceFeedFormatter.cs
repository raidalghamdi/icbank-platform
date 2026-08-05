using System.Globalization;
using System.Text;
using Icbank.Platform.Domain.Gac;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Formats cached GAC social posts and news items into the flat numbered text block the report
/// prompts are built against (BUSINESS-RULES.md §5.1's <c>formatPostsForAI()</c>). Shared by both
/// the editable <c>media_reports</c> generate flow and the immutable <c>final_media_reports</c>
/// generate flow, since both feed the same source tables into an AI prompt.
/// </summary>
public static class SourceFeedFormatter
{
    /// <summary>Formats a set of social posts and news items into one numbered text block, in chronological order.</summary>
    /// <param name="posts">The social posts within the report's date range.</param>
    /// <param name="news">The news items within the report's date range.</param>
    /// <returns>The flat numbered text block, or an empty string if both inputs are empty.</returns>
    public static string Format(IReadOnlyList<GacSocialPost> posts, IReadOnlyList<GacNewsItem> news)
    {
        List<(DateTimeOffset Date, string Line)> entries = new();
        entries.AddRange(posts.Select(p => (p.PostedAt ?? p.CreatedAt, FormatPost(p))));
        entries.AddRange(news.Select(n => (n.PublishedAt ?? n.CreatedAt, FormatNews(n))));

        var ordered = entries.OrderBy(e => e.Date).ToList();
        var builder = new StringBuilder();
        for (var i = 0; i < ordered.Count; i++)
        {
            builder.Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append(". ").Append(ordered[i].Line).Append('\n');
        }

        return builder.ToString();
    }

    private static string FormatPost(GacSocialPost post)
    {
        var content = post.ContentAr ?? post.ContentEn ?? string.Empty;
        return $"[{post.Platform}] {content} ({post.PostUrl})";
    }

    private static string FormatNews(GacNewsItem item)
    {
        var body = item.BodyAr ?? item.BodyEn ?? string.Empty;
        return $"[خبر] {item.TitleAr} - {body} ({item.SourceUrl})";
    }
}
