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
public static class FinalReportHtmlBuilder
{
    private const string Styles = "<style>" +
        "body{font-family:'Arial',sans-serif;direction:rtl;margin:40px;color:#0e3b4a;background:#f5f8f9;}" +
        "h1{color:#1a6e7a;border-bottom:2px solid #1a6e7a;padding-bottom:8px;}" +
        "h2{color:#1a6e7a;margin-top:24px;font-size:16px;border-right:4px solid #b8924a;padding-right:10px;}" +
        ".meta{background:#cce4e6;padding:12px 16px;border-radius:8px;margin:16px 0;font-size:14px;}" +
        "table{width:100%;border-collapse:collapse;margin-top:12px;font-size:13px;}" +
        "th{background:#1a6e7a;color:white;padding:8px 10px;text-align:right;}" +
        "td{padding:7px 10px;border-bottom:1px solid #cce4e6;}" +
        "</style>";

    /// <summary>Builds the full report HTML document.</summary>
    /// <param name="detail">The report detail to render.</param>
    /// <returns>The fully HTML-encoded document.</returns>
    public static string Build(FinalMediaReportDetailDto detail)
    {
        var builder = new StringBuilder();
        builder.Append("<!DOCTYPE html><html dir=\"rtl\" lang=\"ar\"><head><meta charset=\"UTF-8\">").Append(Styles).Append("</head><body>");
        AppendHeader(builder, detail.Summary);
        AppendExecutiveSummary(builder, detail.Summary);
        AppendTopNews(builder, detail.TopNews);
        AppendRecommendations(builder, detail.Recommendations);
        builder.Append("</body></html>");
        return builder.ToString();
    }

    private static void AppendHeader(StringBuilder builder, FinalMediaReportDto summary)
    {
        builder.Append("<h1>").Append(Encode(summary.Title)).Append("</h1><div class=\"meta\">");
        builder.Append("<span>الرقم: <strong>").Append(Encode(summary.ReportNumber)).Append("</strong></span> ");
        builder.Append("<span>الفترة: <strong>").Append(Encode(summary.PeriodLabel)).Append("</strong></span>");
        builder.Append("</div>");
    }

    private static void AppendExecutiveSummary(StringBuilder builder, FinalMediaReportDto summary)
    {
        builder.Append("<h2>الملخص التنفيذي</h2><p>").Append(Encode(summary.ExecutiveSummary ?? string.Empty)).Append("</p>");
    }

    private static void AppendTopNews(StringBuilder builder, IReadOnlyList<TopNewsItemDto> topNews)
    {
        builder.Append("<h2>أبرز الأخبار (").Append(topNews.Count).Append(")</h2>");
        if (topNews.Count == 0)
        {
            builder.Append("<p>لا توجد أخبار مسجلة.</p>");
            return;
        }

        builder.Append("<table><tr><th>التاريخ</th><th>العنوان</th><th>النبرة</th><th>المصدر</th></tr>");
        foreach (TopNewsItemDto item in topNews)
        {
            builder.Append("<tr><td>").Append(Encode(item.Date)).Append("</td><td>").Append(Encode(item.Headline))
                .Append("</td><td>").Append(Encode(item.Tone)).Append("</td><td>").Append(Encode(item.Source)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendRecommendations(StringBuilder builder, IReadOnlyList<RecommendationDto> recommendations)
    {
        builder.Append("<h2>التوصيات (").Append(recommendations.Count).Append(")</h2><ul>");
        foreach (RecommendationDto recommendation in recommendations)
        {
            builder.Append("<li><strong>").Append(Encode(recommendation.Title)).Append("</strong>: ").Append(Encode(recommendation.Description)).Append("</li>");
        }

        builder.Append("</ul>");
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
