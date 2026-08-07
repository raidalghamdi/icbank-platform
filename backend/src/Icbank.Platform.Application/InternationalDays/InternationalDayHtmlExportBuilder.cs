using System.Net;
using System.Text;

namespace Icbank.Platform.Application.InternationalDays;

/// <summary>
/// Builds the Word-compatible HTML export document for an international day. Closes
/// DEFECT-LOG.md SEC-21/H-1: the Node source (<c>international-days.ts:730-805</c>) interpolated
/// AI-generated and user-influenced fields directly into raw HTML template literals with zero
/// escaping. Every value that originates from the database (which may itself have originated
/// from an AI provider) is passed through <see cref="WebUtility.HtmlEncode(string?)"/> here
/// before being placed into the document -- never via raw string interpolation of untrusted
/// content. Only the hardcoded CSS/markup skeleton is not encoded, since it contains no
/// user/AI-influenced values.
/// </summary>
public static class InternationalDayHtmlExportBuilder
{
    private const string Styles = "<style>" +
        "body{font-family:'Arial',sans-serif;direction:rtl;margin:40px;color:#1a1a2e;}" +
        "h1{color:#1e40af;border-bottom:2px solid #1e40af;padding-bottom:8px;}" +
        "h2{color:#1e40af;margin-top:28px;font-size:16px;border-right:4px solid #1e40af;padding-right:10px;}" +
        ".meta{background:#f0f4ff;padding:12px 16px;border-radius:8px;margin:16px 0;font-size:14px;}" +
        ".meta span{margin-left:24px;}" +
        "table{width:100%;border-collapse:collapse;margin-top:12px;font-size:13px;}" +
        "th{background:#1e40af;color:white;padding:8px 10px;text-align:right;}" +
        "td{padding:7px 10px;border-bottom:1px solid #e5e7eb;}" +
        "tr:nth-child(even) td{background:#f8fafc;}" +
        "ul{padding-right:20px;line-height:2;}" +
        ".theme-box{background:#ecfdf5;border:1px solid #6ee7b7;padding:14px;border-radius:8px;margin:12px 0;}" +
        ".footer{margin-top:40px;font-size:11px;color:#9ca3af;text-align:center;border-top:1px solid #e5e7eb;padding-top:12px;}" +
        "</style>";

    /// <summary>Builds the full export HTML document.</summary>
    /// <param name="model">The data to render.</param>
    /// <returns>The fully HTML-encoded document.</returns>
    public static string Build(InternationalDayExportModel model)
    {
        var builder = new StringBuilder();
        builder.Append("<!DOCTYPE html><html dir=\"rtl\" lang=\"ar\"><head><meta charset=\"UTF-8\">");
        builder.Append(Styles);
        builder.Append("</head><body>");
        AppendHeader(builder, model);
        AppendHistory(builder, model);
        AppendTheme(builder, model);
        AppendActivations(builder, model);
        AppendSuggestions(builder, model);
        AppendSources(builder, model);
        builder.Append("<div class=\"footer\">تم التصدير من بنك التواصل الداخلي · ").Append(Encode(model.ExportedAtLabel)).Append("</div>");
        builder.Append("</body></html>");
        return builder.ToString();
    }

    private static void AppendHeader(StringBuilder builder, InternationalDayExportModel model)
    {
        builder.Append("<h1>").Append(Encode(model.DayNameAr));
        if (!string.IsNullOrEmpty(model.DayNameEn))
        {
            builder.Append(" — ").Append(Encode(model.DayNameEn));
        }

        builder.Append("</h1><div class=\"meta\">");
        builder.Append("<span>📅 التاريخ السنوي: <strong>").Append(Encode(model.AnnualDate ?? "غير محدد")).Append("</strong></span>");
        builder.Append("<span>🏛 الجهة الراعية: <strong>").Append(Encode(model.OfficialOrganizer ?? "—")).Append("</strong></span>");
        builder.Append("<span>🏷 الفئة: <strong>").Append(Encode(model.Category ?? "—")).Append("</strong></span>");
        builder.Append("</div>");
    }

    private static void AppendHistory(StringBuilder builder, InternationalDayExportModel model)
    {
        builder.Append("<h2>الملخص التاريخي</h2><p>").Append(Encode(model.HistorySummary ?? "لا يوجد ملخص.")).Append("</p>");
        if (!string.IsNullOrEmpty(model.HistorySource))
        {
            builder.Append("<p style=\"font-size:12px;color:#6b7280\">المصدر: <a href=\"").Append(Encode(model.HistorySource))
                .Append("\">").Append(Encode(model.HistorySource)).Append("</a></p>");
        }
    }

    private static void AppendTheme(StringBuilder builder, InternationalDayExportModel model)
    {
        builder.Append("<h2>شعار ").Append(Encode(model.CurrentYearLabel)).Append("</h2><div class=\"theme-box\">");
        builder.Append("<p><strong>عربي:</strong> ").Append(Encode(model.ThemeAr ?? "⚠️ غير موثق")).Append("</p>");
        builder.Append("<p><strong>English:</strong> ").Append(Encode(model.ThemeEn ?? "N/A")).Append("</p>");
        if (!string.IsNullOrEmpty(model.ThemeSourceUrl))
        {
            builder.Append("<p style=\"font-size:12px\"><a href=\"").Append(Encode(model.ThemeSourceUrl)).Append("\">🔗 المصدر الرسمي</a></p>");
        }

        builder.Append("</div>");
    }

    private static void AppendActivations(StringBuilder builder, InternationalDayExportModel model)
    {
        builder.Append("<h2>تفعيلات سابقة (").Append(model.Activations.Count).Append(")</h2>");
        if (model.Activations.Count == 0)
        {
            builder.Append("<p>لا توجد تفعيلات مسجلة.</p>");
            return;
        }

        builder.Append("<table><tr><th>#</th><th>الجهة</th><th>النوع</th><th>التفعيل</th><th>الوصف</th><th>البلد</th><th>السنة</th><th>المصدر</th></tr>");
        for (var i = 0; i < model.Activations.Count; i++)
        {
            AppendActivationRow(builder, model.Activations[i], i + 1);
        }

        builder.Append("</table>");
    }

    private static void AppendActivationRow(StringBuilder builder, InternationalDayExportActivation activation, int rowNumber)
    {
        builder.Append("<tr><td>").Append(rowNumber).Append("</td>");
        builder.Append("<td>").Append(Encode(activation.EntityName ?? "—")).Append("</td>");
        builder.Append("<td>").Append(Encode(activation.EntityType ?? "—")).Append("</td>");
        builder.Append("<td>").Append(Encode(activation.ActivationType ?? "—")).Append("</td>");
        builder.Append("<td>").Append(Encode(activation.Description ?? "—")).Append("</td>");
        builder.Append("<td>").Append(Encode(activation.Country ?? "—")).Append("</td>");
        builder.Append("<td>").Append(activation.Year?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—").Append("</td>");
        builder.Append("<td>");
        AppendActivationSourceCell(builder, activation);
        builder.Append("</td></tr>");
    }

    private static void AppendActivationSourceCell(StringBuilder builder, InternationalDayExportActivation activation)
    {
        if (!string.IsNullOrEmpty(activation.SourceUrl))
        {
            builder.Append("<a href=\"").Append(Encode(activation.SourceUrl)).Append("\">رابط</a>");
            return;
        }

        builder.Append(activation.Verified ? "موثق" : "⚠️ غير موثق");
    }

    private static void AppendSuggestions(StringBuilder builder, InternationalDayExportModel model)
    {
        builder.Append("<h2>أفكار مقترحة للتفعيل</h2><ul>");
        if (model.Suggestions.Count == 0)
        {
            builder.Append("<li>لا توجد اقتراحات.</li>");
        }
        else
        {
            for (var i = 0; i < model.Suggestions.Count; i++)
            {
                builder.Append("<li>").Append(i + 1).Append(". ").Append(Encode(model.Suggestions[i])).Append("</li>");
            }
        }

        builder.Append("</ul>");
    }

    private static void AppendSources(StringBuilder builder, InternationalDayExportModel model)
    {
        builder.Append("<h2>المصادر (").Append(model.Sources.Count).Append(")</h2><ul>");
        if (model.Sources.Count == 0)
        {
            builder.Append("<li>لا توجد مصادر مسجلة.</li>");
        }
        else
        {
            for (var i = 0; i < model.Sources.Count; i++)
            {
                InternationalDayExportSource source = model.Sources[i];
                builder.Append("<li>").Append(i + 1).Append(". <a href=\"").Append(Encode(source.SourceUrl)).Append("\">")
                    .Append(Encode(source.SourceTitle ?? source.SourceUrl)).Append("</a> — ").Append(Encode(source.SourcePublisher ?? string.Empty)).Append("</li>");
            }
        }

        builder.Append("</ul>");
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
