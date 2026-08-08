using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Renders the official visual-identity statistics composition.</summary>
internal static class IconEventStatsHeroLayout
{
    private const string Accent = "#9DC41A";
    private const string White = "#FFFFFF";

    internal static string Render(IconEventRenderContext context)
    {
        IconEventStatsHeroMetrics metrics = IconEventStatsHeroMetricsFactory.Create(context);
        IReadOnlyList<IconEventStat> stats = ResolveStats(context.Plan.Stats);
        return $"<div class=\"poster stats-hero-layout\" data-fit-mode=\"shrink\" style=\"width:{context.Width}px;height:{context.Height}px;position:relative;overflow:hidden;font-family:Frutiger LT Arabic,Cairo,Tajawal,sans-serif;direction:rtl;color:{White};background-image:url('{IconEventVisualAssets.StatsHeroBackgroundDataUri}');background-size:cover;background-position:center;\">{RenderLogo(context, metrics)}{RenderDepartment(context, metrics)}{RenderHeadline(context, metrics)}{RenderSubtitle(context, metrics)}{RenderStats(stats, metrics)}{RenderHashtag(context, metrics)}</div>";
    }

    private static string RenderDepartment(IconEventRenderContext context, IconEventStatsHeroMetrics metrics)
    {
        var department = context.Input.Department;
        if (string.IsNullOrWhiteSpace(department) || IconEventSizeCatalog.SuppressesChrome(context.Input.Size))
        {
            return string.Empty;
        }

        return $"<div style=\"position:absolute;top:{metrics.DepartmentTop}px;left:{metrics.DepartmentLeft}px;background:{Accent};color:#ffffff;padding:{metrics.DepartmentPaddingV}px {metrics.DepartmentPaddingH}px;border-radius:0;font-weight:800;font-size:{metrics.DepartmentFont}px;line-height:1.1;white-space:nowrap;letter-spacing:-0.2px;\">{IconEventRenderContext.Encode(department)}</div>";
    }

    private static string RenderHashtag(IconEventRenderContext context, IconEventStatsHeroMetrics metrics)
    {
        var hashtag = context.Input.Hashtag;
        if (string.IsNullOrWhiteSpace(hashtag))
        {
            return string.Empty;
        }

        var value = hashtag.StartsWith('#') ? hashtag : "#" + hashtag;
        return $"<div style=\"position:absolute;bottom:{metrics.HashtagBottom}px;left:{metrics.HashtagLeft}px;color:{Accent};font-weight:800;font-size:{metrics.HashtagSize}px;letter-spacing:0;\">{IconEventRenderContext.Encode(value)}</div>";
    }

    private static string RenderHeadline(IconEventRenderContext context, IconEventStatsHeroMetrics metrics) =>
        $"<div style=\"position:absolute;top:{metrics.TitleTop}px;left:50%;transform:translateX(-50%);width:{metrics.TitleMaxWidth}px;color:{White};font-weight:900;font-size:{metrics.TitleSize}px;line-height:1.05;text-align:center;letter-spacing:-0.5px;word-wrap:break-word;overflow-wrap:break-word;\">{context.Headline}</div>";

    private static string RenderLogo(IconEventRenderContext context, IconEventStatsHeroMetrics metrics)
    {
        var logo = IconEventHtmlFragments.RenderLogo(context.Input.LogoUrl, "width:100%;height:auto;display:block;background:transparent", context.Tokens.LogoHeight, context.Input.Size);
        return $"<div style=\"position:absolute;top:{metrics.LogoTop}px;right:{metrics.LogoRight}px;width:{metrics.LogoWidth}px;height:auto;line-height:0;background:transparent;\">{logo}</div>";
    }

    private static string RenderStats(IReadOnlyList<IconEventStat> stats, IconEventStatsHeroMetrics metrics)
    {
        IEnumerable<string> items = stats.Select((stat, index) => RenderStat(stat, index, metrics));
        return $"<div style=\"position:absolute;top:{metrics.StatsTop}px;left:50%;transform:translateX(-50%);display:grid;grid-template-columns:1fr 1fr 1fr;width:{metrics.StatsWidth}px;align-items:start;justify-items:center;\">{string.Concat(items)}</div>";
    }

    private static string RenderStat(IconEventStat stat, int index, IconEventStatsHeroMetrics metrics)
    {
        var divider = index < 2 ? $"<div style=\"position:absolute;left:0;top:{metrics.DividerTop}px;height:{metrics.DividerHeight}px;width:1.5px;background:rgba(255,255,255,0.35);\"></div>" : string.Empty;
        var label = string.IsNullOrEmpty(stat.Label) ? string.Empty : $"<div style=\"color:{White};font-weight:500;font-size:{metrics.LabelSize}px;line-height:1.4;text-align:center;max-width:260px;\">{EncodeLabel(stat.Label)}</div>";
        var icon = IconEventIconLibrary.Render(stat.Icon, metrics.IconSize, Accent);
        return $"<div style=\"display:flex;flex-direction:column;align-items:center;position:relative;width:100%;padding:0 {metrics.StatPadding}px;\">{divider}<div style=\"width:{metrics.IconSize}px;height:{metrics.IconSize}px;color:{Accent};display:flex;align-items:center;justify-content:center;margin-bottom:{metrics.IconMarginBottom}px;\">{icon}</div><div style=\"width:{metrics.LineWidth}px;height:2.5px;background:{Accent};margin-bottom:{metrics.LineMarginBottom}px;border-radius:2px;\"></div><div style=\"color:{White};font-weight:900;font-size:{metrics.ValueSize}px;line-height:1;margin-bottom:{metrics.ValueMarginBottom}px;direction:ltr;font-variant-numeric:tabular-nums;letter-spacing:-3px;\">{IconEventRenderContext.Encode(stat.Value)}</div>{label}</div>";
    }

    private static string RenderSubtitle(IconEventRenderContext context, IconEventStatsHeroMetrics metrics)
    {
        // The planned lead is used rather than the source subtitle: this slot is a fixed-width
        // absolutely positioned band between the headline and the statistics row, so copy longer
        // than a couple of lines grows straight through the numbers underneath it.
        var lead = context.Plan.Lead;
        if (string.IsNullOrWhiteSpace(lead))
        {
            return string.Empty;
        }

        return $"<div style=\"position:absolute;top:{metrics.SubtitleTop}px;left:50%;transform:translateX(-50%);width:{metrics.SubtitleMaxWidth}px;color:{Accent};font-weight:700;font-size:{metrics.SubtitleSize}px;line-height:1.25;text-align:center;word-wrap:break-word;overflow-wrap:break-word;\">{IconEventRenderContext.Encode(lead)}</div>";
    }

    private static List<IconEventStat> ResolveStats(IReadOnlyList<IconEventStat> inputStats)
    {
        List<IconEventStat> stats = inputStats.Count > 0 ? inputStats.Take(3).ToList() : CreateDefaultStats();
        List<IconEventStat> padding = CreateDefaultStats();
        while (stats.Count < 3)
        {
            // Padding entries reuse the neutral defaults so an unfilled column still reads as a
            // placeholder for a real figure instead of a decorative star.
            stats.Add(padding[stats.Count]);
        }

        return stats;
    }

    private static List<IconEventStat> CreateDefaultStats()
    {
        var stats = new List<IconEventStat>();
        stats.Add(new IconEventStat("building", "—", "إدارة"));
        stats.Add(new IconEventStat("users", "—", "مشاركة"));
        stats.Add(new IconEventStat("presentation", "—", "جلسة"));
        return stats;
    }

    private static string EncodeLabel(string label) => IconEventRenderContext.Encode(label).Replace("\n", "<br>", StringComparison.Ordinal);
}
