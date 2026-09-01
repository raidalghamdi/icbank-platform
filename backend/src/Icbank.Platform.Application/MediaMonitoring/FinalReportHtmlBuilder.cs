using System.Net;
using System.Text;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Builds the shared HTML representation of a final media report, used identically by both the
/// PDF-export route and the send-email route (BUSINESS-RULES.md §5.7: "the same rendered
/// document is both the downloadable PDF and the emailed report"). Every value that originates
/// from the database (which may itself have originated from an AI provider) is passed through
/// <see cref="WebUtility.HtmlEncode(string?)"/> before being placed into the document -- never via
/// raw string interpolation of untrusted content (closes the H-1 class of defect the same way
/// <c>InternationalDayHtmlExportBuilder</c> does). Preserves the report-template brand palette
/// exactly (BUSINESS-RULES.md §5.7): TEAL #1a6e7a, NAVY #0e3b4a, MINT #cce4e6, MUSTARD #b8924a,
/// BG #f5f8f9 -- distinct from the main app's DGA-derived palette used elsewhere.
/// </summary>
/// <remarks>
/// The document carries every section the report holds. Exporting only the summary, the news
/// table and the recommendations meant a reader who opened the PDF was missing the timeline,
/// the tone breakdown, the analysis, the alerts, the quotes and the sources the report was
/// assembled from, and had to return to the screen to see them.
/// </remarks>
public static class FinalReportHtmlBuilder
{
    private const string Styles = "<style>" +
        "body{font-family:'Frutiger LT Arabic',sans-serif;direction:rtl;margin:40px;color:#0e3b4a;background:#f5f8f9;}" +
        "h1{color:#1a6e7a;border-bottom:2px solid #1a6e7a;padding-bottom:8px;}" +
        "h2{color:#1a6e7a;margin-top:24px;font-size:16px;border-right:4px solid #b8924a;padding-right:10px;}" +
        ".meta{background:#cce4e6;padding:12px 16px;border-radius:8px;margin:16px 0;font-size:14px;}" +
        ".quote{border-right:4px solid #b8924a;padding:8px 12px;font-style:italic;}" +
        "table{width:100%;border-collapse:collapse;margin-top:12px;font-size:13px;}" +
        "th{background:#1a6e7a;color:white;padding:8px 10px;text-align:right;}" +
        "td{padding:7px 10px;border-bottom:1px solid #cce4e6;}" +
        "</style>";

    /// <summary>Builds the full report HTML document.</summary>
    /// <param name="detail">The report detail to render.</param>
    /// <returns>The fully HTML-encoded document.</returns>
    public static string Build(FinalMediaReportDetailDto detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        var builder = new StringBuilder();
        builder.Append("<!DOCTYPE html><html dir=\"rtl\" lang=\"ar\"><head><meta charset=\"UTF-8\">").Append(Styles).Append("</head><body>");
        AppendHeader(builder, detail.Summary);
        AppendExecutiveSummary(builder, detail.Summary);
        AppendTopNews(builder, detail.TopNews);
        AppendTimeline(builder, detail.Timeline);
        AppendDigitalPresence(builder, detail.DigitalPresence);
        AppendEditorialTone(builder, detail.EditorialTone);
        AppendDeepAnalysis(builder, detail.DeepAnalysis);
        AppendRegionalComparison(builder, detail.RegionalComparison);
        AppendRecommendations(builder, detail.Recommendations);
        AppendAlerts(builder, detail.Alerts);
        AppendQuotes(builder, detail.QuotesAppendix);
        AppendMethodology(builder, detail.Methodology);
        AppendSources(builder, detail.Sources);
        builder.Append("</body></html>");
        return builder.ToString();
    }

    private static void AppendHeader(StringBuilder builder, FinalMediaReportDto summary)
    {
        builder.Append("<h1>").Append(Encode(summary.Title)).Append("</h1><div class=\"meta\">");
        builder.Append("<span>الرقم: <strong>").Append(Encode(summary.ReportNumber)).Append("</strong></span> ");
        builder.Append("<span>الفترة: <strong>").Append(Encode(summary.PeriodLabel)).Append("</strong></span>");
        builder.Append("</div>");
        AppendKpis(builder, summary.Kpis);
    }

    private static void AppendKpis(StringBuilder builder, ReportKpisDto kpis)
    {
        var rows = new List<(string Label, string Value)>();
        AddKpi(rows, "إجمالي الأخبار", kpis.TotalNews);
        AddKpi(rows, "نسبة الإيجابية", kpis.PositivePercent, "%");
        AddKpi(rows, "المنافذ الإعلامية", kpis.MediaOutlets);
        AddKpi(rows, "المواضيع الرئيسية", kpis.KeyTopics);
        AddKpi(rows, "عدد التنبيهات", kpis.AlertsCount);
        if (!string.IsNullOrWhiteSpace(kpis.Reach))
        {
            rows.Add(("مدى الوصول", kpis.Reach!));
        }

        if (rows.Count == 0)
        {
            return;
        }

        AppendHeading(builder, "مؤشرات الفترة", rows.Count);
        builder.Append("<table data-widths=\"2,1\"><tr><th>المؤشر</th><th>القيمة</th></tr>");
        foreach ((var label, var value) in rows)
        {
            builder.Append("<tr><td>").Append(Encode(label)).Append("</td><td>").Append(Encode(value)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AddKpi(List<(string Label, string Value)> rows, string label, int? value, string suffix = "")
    {
        if (value.HasValue)
        {
            rows.Add((label, value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + suffix));
        }
    }

    private static void AppendExecutiveSummary(StringBuilder builder, FinalMediaReportDto summary)
    {
        if (string.IsNullOrWhiteSpace(summary.ExecutiveSummary))
        {
            return;
        }

        builder.Append("<h2>الملخص التنفيذي</h2><p>").Append(Encode(summary.ExecutiveSummary)).Append("</p>");
    }

    private static void AppendTopNews(StringBuilder builder, IReadOnlyList<TopNewsItemDto> topNews)
    {
        if (topNews.Count == 0)
        {
            builder.Append("<h2>أبرز الأخبار</h2><p>لا توجد أخبار مسجلة.</p>");
            return;
        }

        AppendHeading(builder, "أبرز الأخبار", topNews.Count);
        builder.Append("<table data-widths=\"2,4.4,1,1.8\"><tr><th>التاريخ</th><th>العنوان</th><th>النبرة</th><th>المصدر</th></tr>");
        foreach (TopNewsItemDto item in topNews)
        {
            var headline = item.Details.Count == 0
                ? item.Headline
                : item.Headline + " — " + string.Join(" ", item.Details);
            builder.Append("<tr><td>").Append(Encode(item.Date)).Append("</td><td>").Append(Encode(headline))
                .Append("</td><td>").Append(Encode(item.Tone)).Append("</td><td>").Append(Encode(item.Source)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendTimeline(StringBuilder builder, IReadOnlyList<TimelineEventDto> timeline)
    {
        if (timeline.Count == 0)
        {
            return;
        }

        AppendHeading(builder, "الخط الزمني للتغطية", timeline.Count);
        builder.Append("<table data-widths=\"2,4,1.8,1,0.8\"><tr><th>التاريخ</th><th>الحدث</th><th>المنفذ</th><th>النبرة</th><th>العدد</th></tr>");
        foreach (TimelineEventDto item in timeline)
        {
            builder.Append("<tr><td>").Append(Encode(item.Date)).Append("</td><td>").Append(Encode(item.Event))
                .Append("</td><td>").Append(Encode(item.Outlet)).Append("</td><td>").Append(Encode(item.Tone))
                .Append("</td><td>").Append(Number(item.Count)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendDigitalPresence(StringBuilder builder, DigitalPresenceDto presence)
    {
        if (presence.Platforms.Count > 0)
        {
            AppendHeading(builder, "الحضور الرقمي", presence.Platforms.Count);
            builder.Append("<table data-widths=\"2,1,1,1.2,1.4\"><tr><th>المنصة</th><th>الإشارات</th><th>إعادة النشر</th><th>التفاعل</th><th>الوصول</th></tr>");
            foreach (DigitalPresencePlatformDto platform in presence.Platforms)
            {
                builder.Append("<tr><td>").Append(Encode(platform.Name)).Append("</td><td>").Append(Number(platform.Mentions))
                    .Append("</td><td>").Append(Number(platform.Reposts)).Append("</td><td>").Append(Number(platform.Engagement))
                    .Append("</td><td>").Append(Encode(platform.Reach)).Append("</td></tr>");
            }

            builder.Append("</table>");
        }

        if (presence.Hashtags.Count == 0)
        {
            return;
        }

        AppendHeading(builder, "الوسوم الأكثر استخداماً", presence.Hashtags.Count);
        builder.Append("<table data-widths=\"3,1,1.4\"><tr><th>الوسم</th><th>الاستخدامات</th><th>الاتجاه</th></tr>");
        foreach (DigitalPresenceHashtagDto hashtag in presence.Hashtags)
        {
            builder.Append("<tr><td>").Append(Encode(hashtag.Tag)).Append("</td><td>").Append(Number(hashtag.Uses))
                .Append("</td><td>").Append(Encode(hashtag.Trend)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendEditorialTone(StringBuilder builder, EditorialToneDto tone)
    {
        AppendToneBuckets(builder, "توزيع النبرة التحريرية", tone.Distribution);
        AppendToneBuckets(builder, "تصنيف التغطية", tone.Classification);
        AppendToneBuckets(builder, "النبرة حسب المصدر", tone.Sources);
    }

    private static void AppendToneBuckets(StringBuilder builder, string title, IReadOnlyList<EditorialToneBucketDto> buckets)
    {
        if (buckets.Count == 0)
        {
            return;
        }

        AppendHeading(builder, title, buckets.Count);
        builder.Append("<table data-widths=\"3,1,1\"><tr><th>البند</th><th>النسبة</th><th>العدد</th></tr>");
        foreach (EditorialToneBucketDto bucket in buckets)
        {
            builder.Append("<tr><td>").Append(Encode(bucket.Label)).Append("</td><td>")
                .Append(Encode(bucket.Percent.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%"))
                .Append("</td><td>").Append(Number(bucket.Count)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendDeepAnalysis(StringBuilder builder, DeepAnalysisDto analysis)
    {
        if (analysis.Keywords.Count > 0)
        {
            AppendHeading(builder, "الكلمات المفتاحية", analysis.Keywords.Count);
            builder.Append("<table data-widths=\"1.6,0.8,4\"><tr><th>الكلمة</th><th>التكرار</th><th>السياق</th></tr>");
            foreach (DeepAnalysisKeywordDto keyword in analysis.Keywords)
            {
                builder.Append("<tr><td>").Append(Encode(keyword.Keyword)).Append("</td><td>").Append(Number(keyword.Frequency))
                    .Append("</td><td>").Append(Encode(keyword.Context)).Append("</td></tr>");
            }

            builder.Append("</table>");
        }

        if (analysis.Quote is not null)
        {
            builder.Append("<h2>تصريح بارز</h2><div class=\"quote\">").Append(Encode(analysis.Quote.Text))
                .Append(" — ").Append(Encode(analysis.Quote.Source)).Append(" (").Append(Encode(analysis.Quote.Date)).Append(")</div>");
        }

        AppendBulletList(builder, "عناصر القوة", analysis.Strengths);
        AppendBulletList(builder, "نقاط تحتاج معالجة", analysis.Weaknesses);
    }

    private static void AppendBulletList(StringBuilder builder, string title, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        AppendHeading(builder, title, items.Count);
        builder.Append("<ul>");
        foreach (var item in items)
        {
            builder.Append("<li>").Append(Encode(item)).Append("</li>");
        }

        builder.Append("</ul>");
    }

    private static void AppendRegionalComparison(StringBuilder builder, IReadOnlyList<RegionalComparisonDto> comparison)
    {
        if (comparison.Count == 0)
        {
            return;
        }

        AppendHeading(builder, "المقارنة الإقليمية", comparison.Count);
        builder.Append("<table data-widths=\"2,1.2,1,1,3.4\"><tr><th>الجهة</th><th>الدولة</th><th>الإشارات</th><th>النبرة</th><th>أبرز ما ورد</th></tr>");
        foreach (RegionalComparisonDto item in comparison)
        {
            builder.Append("<tr><td>").Append(Encode(item.Authority)).Append("</td><td>").Append(Encode(item.Country))
                .Append("</td><td>").Append(Number(item.Mentions)).Append("</td><td>").Append(Encode(item.Tone))
                .Append("</td><td>").Append(Encode(item.Highlights)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendRecommendations(StringBuilder builder, IReadOnlyList<RecommendationDto> recommendations)
    {
        if (recommendations.Count == 0)
        {
            return;
        }

        AppendHeading(builder, "التوصيات", recommendations.Count);
        foreach (RecommendationDto recommendation in recommendations)
        {
            builder.Append("<p><strong>").Append(Encode(recommendation.Title)).Append("</strong>: ")
                .Append(Encode(recommendation.Description)).Append("</p>");
            AppendRecommendationDetails(builder, recommendation);
        }
    }

    private static void AppendRecommendationDetails(StringBuilder builder, RecommendationDto recommendation)
    {
        var parts = new List<string>();
        AddPart(parts, "الأولوية", recommendation.Priority);
        AddPart(parts, "المسؤول", recommendation.Responsible);
        AddPart(parts, "المؤشر", recommendation.Kpi);
        AddPart(parts, "الموعد", recommendation.Deadline);
        AddPart(parts, "المتطلبات", recommendation.Dependencies);
        if (parts.Count > 0)
        {
            builder.Append("<div class=\"meta\">").Append(Encode(string.Join(" · ", parts))).Append("</div>");
        }
    }

    private static void AddPart(List<string> parts, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add(label + ": " + value);
        }
    }

    private static void AppendAlerts(StringBuilder builder, IReadOnlyList<AlertItemDto> alerts)
    {
        if (alerts.Count == 0)
        {
            return;
        }

        AppendHeading(builder, "التنبيهات والموقف المقترح", alerts.Count);
        builder.Append("<table data-widths=\"1,1\"><tr><th>التنبيه</th><th>الموقف المقترح</th></tr>");
        foreach (AlertItemDto alert in alerts)
        {
            builder.Append("<tr><td>").Append(Encode(alert.Alert)).Append("</td><td>")
                .Append(Encode(alert.SuggestedPosition)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendQuotes(StringBuilder builder, IReadOnlyList<QuoteAppendixItemDto> quotes)
    {
        if (quotes.Count == 0)
        {
            return;
        }

        AppendHeading(builder, "ملحق التصريحات", quotes.Count);
        builder.Append("<table data-widths=\"4.5,1.6,1.5,1.6\"><tr><th>التصريح</th><th>المصدر</th><th>التاريخ</th><th>الموضوع</th></tr>");
        foreach (QuoteAppendixItemDto quote in quotes)
        {
            builder.Append("<tr><td>").Append(Encode(quote.Quote)).Append("</td><td>").Append(Encode(quote.Source))
                .Append("</td><td>").Append(Encode(quote.Date)).Append("</td><td>").Append(Encode(quote.Topic)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendMethodology(StringBuilder builder, string? methodology)
    {
        if (string.IsNullOrWhiteSpace(methodology))
        {
            return;
        }

        builder.Append("<h2>المنهجية</h2><p>").Append(Encode(methodology)).Append("</p>");
    }

    private static void AppendSources(StringBuilder builder, IReadOnlyList<SourceRefDto> sources)
    {
        if (sources.Count == 0)
        {
            return;
        }

        AppendHeading(builder, "المصادر", sources.Count);
        builder.Append("<ul>");
        foreach (SourceRefDto source in sources)
        {
            builder.Append("<li>").Append(Encode(source.Name));
            if (!string.IsNullOrWhiteSpace(source.Description))
            {
                builder.Append(" — ").Append(Encode(source.Description));
            }

            if (!string.IsNullOrWhiteSpace(source.Url))
            {
                builder.Append(" (").Append(Encode(source.Url)).Append(')');
            }

            builder.Append("</li>");
        }

        builder.Append("</ul>");
    }

    private static void AppendHeading(StringBuilder builder, string title, int count) =>
        builder.Append("<h2>").Append(Encode(title)).Append(" (").Append(Number(count)).Append(")</h2>");

    private static string Number(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
