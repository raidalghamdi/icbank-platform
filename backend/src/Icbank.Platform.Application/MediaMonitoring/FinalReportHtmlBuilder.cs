using System.Globalization;
using System.Net;
using System.Text;
using Icbank.Platform.Application.MediaMonitoring.Appearance;

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
/// The document follows the authority's approved media-monitoring layout: a cover sheet, then six
/// numbered sections (executive summary and indicators, the period's news, editorial-direction
/// analysis, deep analysis, recommendations and alerts, methodology and sources). A report that
/// carried the same content in a different shape was not recognisable as the authority's own
/// document and could not be circulated as one.
/// </remarks>
public static class FinalReportHtmlBuilder
{
    private const string OrganisationArabic = "الهيئة العامة للمنافسة";
    private const string OrganisationEnglish = "General Authority for Competition";
    private const string Confidentiality = "سري — للاستخدام الداخلي";
    private const string PreparedBy = "الإدارة التنفيذية للتواصل المؤسسي";
    private const string PreparedFor = "الإدارة العليا للهيئة";
    private const int HighlightCount = 5;

    private const string Styles = "<style>" +
        "body{font-family:'Frutiger LT Arabic',sans-serif;direction:rtl;margin:40px;color:#0e3b4a;background:#ffffff;}" +
        "h1{color:#0e3b4a;font-size:30px;margin:16px 0 0;}" +
        "h2{color:#0e3b4a;margin-top:28px;font-size:17px;background:#eef2f3;padding:10px 14px;border-left:5px solid #b8924a;}" +
        "h3{color:#1a6e7a;margin-top:20px;font-size:14px;}" +
        ".cover{border-bottom:5px solid #b8924a;padding-bottom:24px;margin-bottom:24px;}" +
        ".cover-subtitle{color:#1a6e7a;font-size:15px;margin-top:8px;}" +
        ".cover-meta{margin-top:24px;}" +
        ".cover-meta td:first-child{background:#eef2f3;font-weight:bold;width:28%;}" +
        ".kpi-grid{display:flex;flex-wrap:wrap;gap:9px;margin-top:12px;}" +
        ".kpi-grid>div{flex:1 1 30%;background:#f7f9fa;border-top:3px solid #1a6e7a;padding:12px;text-align:center;}" +
        ".kpi-value{display:block;font-size:22px;font-weight:bold;color:#1a6e7a;}" +
        ".kpi-label{display:block;font-weight:bold;margin-top:4px;}" +
        ".kpi-sub{display:block;font-size:11px;color:#6b7b80;margin-top:2px;}" +
        ".news-item{margin-top:14px;}" +
        ".news-headline{color:#1a6e7a;font-weight:bold;font-size:14px;}" +
        ".news-meta{color:#6b7b80;font-size:12px;margin-top:4px;}" +
        ".news-source{color:#b8924a;font-weight:bold;font-size:12px;margin-top:5px;}" +
        ".quote{background:#f7f9fa;border-right:4px solid #b8924a;padding:13px;font-style:italic;}" +
        ".quote-by{color:#6b7b80;font-size:13px;margin-top:5px;}" +
        ".note{background:#f7f9fa;padding:11px;color:#6b7b80;font-size:13px;margin-top:12px;}" +
        ".source-item{margin-top:7px;}" +
        ".source-url{color:#1a6e7a;font-size:12px;direction:ltr;text-align:left;}" +
        ".meta{background:#cce4e6;padding:12px 16px;margin:16px 0;font-size:14px;}" +
        "table{width:100%;border-collapse:collapse;margin-top:12px;font-size:13px;}" +
        "th{background:#1a6e7a;color:white;padding:8px 10px;text-align:right;}" +
        "td{padding:7px 10px;border-bottom:1px solid #dfe6e8;}" +
        "</style>";

    /// <summary>Builds the full report HTML document.</summary>
    /// <param name="detail">The report detail to render.</param>
    /// <returns>The fully HTML-encoded document.</returns>
    public static string Build(FinalMediaReportDetailDto detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        var builder = new StringBuilder();
        builder.Append("<!DOCTYPE html><html dir=\"rtl\" lang=\"ar\"><head><meta charset=\"UTF-8\">").Append(Styles).Append("</head><body>");
        AppendCover(builder, detail.Summary);
        AppendSectionOne(builder, detail);
        AppendSectionTwo(builder, detail);
        AppendSectionThree(builder, detail);
        AppendSectionFour(builder, detail);
        AppendSectionFive(builder, detail);
        AppendSectionSix(builder, detail);
        builder.Append("</body></html>");
        return builder.ToString();
    }

    private static void AppendCover(StringBuilder builder, FinalMediaReportDto summary)
    {
        builder.Append("<div class=\"cover\" data-org=\"").Append(Encode(OrganisationArabic))
            .Append("\" data-org-en=\"").Append(Encode(OrganisationEnglish))
            .Append("\" data-kicker=\"").Append(Encode("تقرير داخلي — للاستخدام المؤسسي"))
            .Append("\" data-confidentiality=\"").Append(Encode(Confidentiality))
            .Append("\" data-report-number=\"").Append(Encode(summary.ReportNumber)).Append("\">");
        builder.Append("<h1>").Append(Encode(summary.Title)).Append("</h1>");
        builder.Append("<div class=\"cover-subtitle\">").Append(Encode("تحليل التغطية الإعلامية للهيئة العامة للمنافسة")).Append("</div>");
        builder.Append("<table class=\"cover-meta\">");
        AppendCoverRow(builder, "الفترة الزمنية", PeriodOf(summary));
        AppendCoverRow(builder, "الجهة المعدة", PreparedBy);
        AppendCoverRow(builder, "الجهة المستفيدة", PreparedFor);
        AppendCoverRow(builder, "الرقم المرجعي", summary.ReportNumber);
        AppendCoverRow(builder, "تاريخ الإصدار", Date(summary.CreatedAt));
        builder.Append("</table></div>");
    }

    private static void AppendCoverRow(StringBuilder builder, string label, string value) =>
        builder.Append("<tr><td>").Append(Encode(label)).Append("</td><td>").Append(Encode(value)).Append("</td></tr>");

    private static string PeriodOf(FinalMediaReportDto summary) =>
        string.IsNullOrWhiteSpace(summary.PeriodLabel)
            ? Date(summary.DateFrom) + " — " + Date(summary.DateTo)
            : summary.PeriodLabel;

    private static void AppendSectionOne(StringBuilder builder, FinalMediaReportDetailDto detail)
    {
        AppendSection(builder, 1, "الملخص التنفيذي");
        if (!string.IsNullOrWhiteSpace(detail.Summary.ExecutiveSummary))
        {
            builder.Append("<p>").Append(Encode(detail.Summary.ExecutiveSummary)).Append("</p>");
        }

        var highlights = detail.TopNews.Take(HighlightCount).Select(item => item.Headline).ToList();
        if (highlights.Count > 0)
        {
            AppendSubHeading(builder, "أبرز المحاور خلال الفترة");
            AppendBulletList(builder, highlights);
        }

        AppendKpiGrid(builder, detail.Summary.Kpis, detail.Appearance);
    }

    /// <summary>
    /// Prints the indicator cards, preferring the counted archive figures over the model's own
    /// count for the two indicators we can measure exactly.
    /// </summary>
    /// <param name="builder">The document builder.</param>
    /// <param name="kpis">The stored indicators.</param>
    /// <param name="appearance">The measured appearance analysis.</param>
    private static void AppendKpiGrid(StringBuilder builder, ReportKpisDto kpis, MediaAppearanceAnalysisDto appearance)
    {
        List<(string Value, string Label, string Sub, string Accent)> cards = BuildKpiCards(kpis, appearance);
        if (cards.Count == 0)
        {
            return;
        }

        AppendSubHeading(builder, "المؤشرات الإعلامية الرئيسية");
        builder.Append("<div class=\"kpi-grid\">");
        foreach ((var value, var label, var sub, var accent) in cards)
        {
            builder.Append("<div data-accent=\"").Append(accent).Append("\">")
                .Append("<div class=\"kpi-value\">").Append(Encode(value)).Append("</div>")
                .Append("<div class=\"kpi-label\">").Append(Encode(label)).Append("</div>")
                .Append("<div class=\"kpi-sub\">").Append(Encode(sub)).Append("</div></div>");
        }

        builder.Append("</div>");
    }

    private static List<(string Value, string Label, string Sub, string Accent)> BuildKpiCards(
        ReportKpisDto kpis,
        MediaAppearanceAnalysisDto appearance)
    {
        var measured = appearance.TotalAppearances > 0;
        var cards = new List<(string Value, string Label, string Sub, string Accent)>();
        AddCard(cards, measured ? appearance.TotalAppearances : kpis.TotalNews, string.Empty, "خبر منشور", "إجمالي الأخبار المرصودة", "teal");
        AddCard(cards, kpis.PositivePercent, "٪", "تغطية إيجابية", "من إجمالي التغطية", "green");
        AddCard(cards, measured ? appearance.DistinctOutlets : kpis.MediaOutlets, string.Empty, "وسيلة إعلامية", "منافذ نشرت عن الهيئة", "mustard");
        AddCard(cards, kpis.KeyTopics, string.Empty, "موضوعات رئيسية", "محاور التغطية الأبرز", "teal");
        if (!string.IsNullOrWhiteSpace(kpis.Reach))
        {
            cards.Add((kpis.Reach!, "وصول جماهيري", "مدى الوصول التقديري", "green"));
        }

        AddCard(cards, kpis.AlertsCount, string.Empty, "تنبيهات للمتابعة", "بنود تستوجب المتابعة", "magenta");
        return cards;
    }

    private static void AddCard(
        List<(string Value, string Label, string Sub, string Accent)> cards,
        int? value,
        string suffix,
        string label,
        string sub,
        string accent)
    {
        if (value.HasValue)
        {
            cards.Add((Number(value.Value) + suffix, label, sub, accent));
        }
    }

    private static void AppendSectionTwo(StringBuilder builder, FinalMediaReportDetailDto detail)
    {
        AppendSection(builder, 2, "أبرز الأخبار خلال الفترة");
        if (detail.TopNews.Count == 0)
        {
            builder.Append("<p>").Append(Encode("لا توجد أخبار مسجلة.")).Append("</p>");
        }

        var index = 1;
        foreach (TopNewsItemDto item in detail.TopNews)
        {
            AppendNewsItem(builder, item, index++);
        }

        AppendTimeline(builder, detail.Timeline);
    }

    private static void AppendNewsItem(StringBuilder builder, TopNewsItemDto item, int index)
    {
        builder.Append("<div class=\"news-item\" data-index=\"").Append(Number(index)).Append("\">");
        builder.Append("<div class=\"news-headline\">").Append(Encode(item.Headline)).Append("</div>");
        builder.Append("<div class=\"news-meta\">").Append(Encode("التاريخ: " + item.Date + " — النبرة: " + item.Tone)).Append("</div>");
        foreach (var paragraph in item.Details)
        {
            builder.Append("<p>").Append(Encode(paragraph)).Append("</p>");
        }

        builder.Append("<div class=\"news-source\">").Append(Encode("المصدر: " + item.Source)).Append("</div></div>");
    }

    private static void AppendTimeline(StringBuilder builder, IReadOnlyList<TimelineEventDto> timeline)
    {
        if (timeline.Count == 0)
        {
            return;
        }

        AppendSubHeading(builder, "الخط الزمني للتغطية");
        builder.Append("<table data-widths=\"1.6,4,1.8,1,0.8\"><tr><th>التاريخ</th><th>الحدث</th><th>الوسيلة</th><th>النبرة</th><th>العدد</th></tr>");
        foreach (TimelineEventDto item in timeline)
        {
            builder.Append("<tr><td>").Append(Encode(item.Date)).Append("</td><td>").Append(Encode(item.Event))
                .Append("</td><td>").Append(Encode(item.Outlet)).Append("</td><td>").Append(Encode(item.Tone))
                .Append("</td><td>").Append(Number(item.Count)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendSectionThree(StringBuilder builder, FinalMediaReportDetailDto detail)
    {
        AppendSection(builder, 3, "تحليل التوجه الإعلامي");
        AppendToneBuckets(builder, "توزع نبرة التغطية الإعلامية", detail.EditorialTone.Distribution);
        AppendToneBuckets(builder, "التصنيف الموضوعي للأخبار", detail.EditorialTone.Classification);
        AppendToneBuckets(builder, "توزع التغطية حسب المصدر", detail.EditorialTone.Sources);
        AppendMeasuredAppearance(builder, detail.Appearance);
        AppendTopOutlets(builder, detail.Appearance);
        AppendDigitalPresence(builder, detail.DigitalPresence);
    }

    private static void AppendToneBuckets(StringBuilder builder, string title, IReadOnlyList<EditorialToneBucketDto> buckets)
    {
        if (buckets.Count == 0)
        {
            return;
        }

        AppendSubHeading(builder, title);
        builder.Append("<table data-widths=\"3,1,1\"><tr><th>البند</th><th>عدد الأخبار</th><th>النسبة</th></tr>");
        foreach (EditorialToneBucketDto bucket in buckets)
        {
            builder.Append("<tr><td>").Append(Encode(bucket.Label)).Append("</td><td>").Append(Number(bucket.Count))
                .Append("</td><td>").Append(Encode(bucket.Percent.ToString("0.#", CultureInfo.InvariantCulture) + "٪"))
                .Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    /// <summary>
    /// Prints the figures counted from the monitored archive. These replace the model-authored
    /// engagement numbers as the section's factual core: the prompt is not allowed to invent
    /// statistics, so without a social listening feed it returned zeros for every platform metric.
    /// </summary>
    /// <param name="builder">The document builder.</param>
    /// <param name="appearance">The measured appearance analysis.</param>
    private static void AppendMeasuredAppearance(StringBuilder builder, MediaAppearanceAnalysisDto appearance)
    {
        if (appearance.TotalAppearances == 0)
        {
            return;
        }

        AppendSubHeading(builder, "قياس الظهور الإعلامي خلال الفترة");
        builder.Append("<table data-widths=\"2,1\"><tr><th>المؤشر</th><th>القيمة</th></tr>");
        AppendCoverRow(builder, "إجمالي مرات الظهور المرصودة", Number(appearance.TotalAppearances));
        AppendCoverRow(builder, "الظهور في المنافذ الصحفية", Number(appearance.PressAppearances));
        AppendCoverRow(builder, "عدد المنافذ التي نشرت عن الهيئة", Number(appearance.DistinctOutlets));
        AppendCoverRow(builder, "أيام حملت تغطية", Number(appearance.ActiveDays));
        AppendCoverRow(builder, "متوسط الظهور في اليوم", appearance.AveragePerDay.ToString("0.#", CultureInfo.InvariantCulture));
        if (appearance.PeakDay is not null)
        {
            AppendCoverRow(builder, "أعلى يوم تغطية", appearance.PeakDay + " (" + Number(appearance.PeakDayAppearances) + ")");
        }

        AppendCoverRow(
            builder,
            "الظهور على منصات التواصل",
            appearance.HasSocialData ? Number(appearance.SocialAppearances) : "لا يوجد مصدر رصد مرتبط");
        builder.Append("</table>");
    }

    private static void AppendTopOutlets(StringBuilder builder, MediaAppearanceAnalysisDto appearance)
    {
        if (appearance.TopOutlets.Count == 0)
        {
            return;
        }

        AppendSubHeading(builder, "المنافذ الأكثر نشراً عن الهيئة");
        builder.Append("<table data-widths=\"3,1,1\"><tr><th>المنفذ</th><th>مرات الظهور</th><th>النسبة</th></tr>");
        foreach (MediaAppearanceOutletDto outlet in appearance.TopOutlets)
        {
            builder.Append("<tr><td>").Append(Encode(outlet.Name)).Append("</td><td>").Append(Number(outlet.Appearances))
                .Append("</td><td>").Append(Encode(Number(outlet.SharePercent) + "٪")).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendDigitalPresence(StringBuilder builder, DigitalPresenceDto presence)
    {
        var platforms = presence.Platforms
            .Where(p => p.Mentions > 0 || p.Reposts > 0 || p.Engagement > 0)
            .ToList();
        if (platforms.Count > 0)
        {
            AppendSubHeading(builder, "الحضور الرقمي");
            builder.Append("<table data-widths=\"2,1,1,1.2,1.4\"><tr><th>المنصة</th><th>الإشارات</th><th>إعادة النشر</th><th>التفاعل</th><th>الوصول</th></tr>");
            foreach (DigitalPresencePlatformDto platform in platforms)
            {
                builder.Append("<tr><td>").Append(Encode(platform.Name)).Append("</td><td>").Append(Number(platform.Mentions))
                    .Append("</td><td>").Append(Number(platform.Reposts)).Append("</td><td>").Append(Number(platform.Engagement))
                    .Append("</td><td>").Append(Encode(platform.Reach)).Append("</td></tr>");
            }

            builder.Append("</table>");
        }

        var hashtags = presence.Hashtags.Where(h => h.Uses > 0).ToList();
        if (hashtags.Count == 0)
        {
            return;
        }

        AppendSubHeading(builder, "الوسوم الأكثر استخداماً");
        builder.Append("<table data-widths=\"3,1,1.4\"><tr><th>الوسم</th><th>الاستخدامات</th><th>الاتجاه</th></tr>");
        foreach (DigitalPresenceHashtagDto hashtag in hashtags)
        {
            builder.Append("<tr><td>").Append(Encode(hashtag.Tag)).Append("</td><td>").Append(Number(hashtag.Uses))
                .Append("</td><td>").Append(Encode(hashtag.Trend)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendSectionFour(StringBuilder builder, FinalMediaReportDetailDto detail)
    {
        AppendSection(builder, 4, "تحليل عميق ومؤشرات قطاعية");
        AppendKeywords(builder, detail.DeepAnalysis.Keywords);
        AppendQuote(builder, detail.DeepAnalysis.Quote);
        AppendStrategicReading(builder, detail.DeepAnalysis.Strengths, detail.DeepAnalysis.Weaknesses);
        AppendRegionalComparison(builder, detail.RegionalComparison);
    }

    private static void AppendKeywords(StringBuilder builder, IReadOnlyList<DeepAnalysisKeywordDto> keywords)
    {
        if (keywords.Count == 0)
        {
            return;
        }

        AppendSubHeading(builder, "أبرز الكلمات المفتاحية في التغطية");
        builder.Append("<table data-widths=\"1.6,0.8,4\"><tr><th>الكلمة المفتاحية</th><th>التكرار</th><th>السياق الغالب</th></tr>");
        foreach (DeepAnalysisKeywordDto keyword in keywords)
        {
            builder.Append("<tr><td>").Append(Encode(keyword.Keyword)).Append("</td><td>").Append(Number(keyword.Frequency))
                .Append("</td><td>").Append(Encode(keyword.Context)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendQuote(StringBuilder builder, DeepAnalysisQuoteDto? quote)
    {
        if (quote is null)
        {
            return;
        }

        AppendSubHeading(builder, "اقتباس بارز من التغطية");
        builder.Append("<div class=\"quote\">").Append(Encode(quote.Text)).Append("</div>");
        builder.Append("<div class=\"quote-by\">").Append(Encode("— " + quote.Source + "، " + quote.Date)).Append("</div>");
    }

    private static void AppendStrategicReading(StringBuilder builder, IReadOnlyList<string> strengths, IReadOnlyList<string> weaknesses)
    {
        if (strengths.Count == 0 && weaknesses.Count == 0)
        {
            return;
        }

        AppendSubHeading(builder, "قراءة استراتيجية للحضور الإعلامي");
        builder.Append("<table data-widths=\"1,1\"><tr><th data-tone=\"green\">نقاط القوة الإعلامية</th>")
            .Append("<th data-tone=\"magenta\">نقاط تتطلب الانتباه</th></tr>");
        for (var index = 0; index < Math.Max(strengths.Count, weaknesses.Count); index++)
        {
            builder.Append("<tr><td>").Append(Encode(index < strengths.Count ? strengths[index] : string.Empty))
                .Append("</td><td>").Append(Encode(index < weaknesses.Count ? weaknesses[index] : string.Empty))
                .Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendRegionalComparison(StringBuilder builder, IReadOnlyList<RegionalComparisonDto> comparison)
    {
        if (comparison.Count == 0)
        {
            return;
        }

        AppendSubHeading(builder, "المقارنة الإقليمية");
        builder.Append("<table data-widths=\"2,1.2,1,1,3.4\"><tr><th>الجهة</th><th>الدولة</th><th>الإشارات</th><th>النبرة</th><th>أبرز ما ورد</th></tr>");
        foreach (RegionalComparisonDto item in comparison)
        {
            builder.Append("<tr><td>").Append(Encode(item.Authority)).Append("</td><td>").Append(Encode(item.Country))
                .Append("</td><td>").Append(Number(item.Mentions)).Append("</td><td>").Append(Encode(item.Tone))
                .Append("</td><td>").Append(Encode(item.Highlights)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendSectionFive(StringBuilder builder, FinalMediaReportDetailDto detail)
    {
        AppendSection(builder, 5, "التوصيات والإجراءات المقترحة");
        AppendRecommendations(builder, detail.Recommendations);
        AppendAlerts(builder, detail.Alerts);
    }

    private static void AppendRecommendations(StringBuilder builder, IReadOnlyList<RecommendationDto> recommendations)
    {
        if (recommendations.Count == 0)
        {
            return;
        }

        builder.Append("<p>").Append(Encode("بناء على تحليل التغطية الإعلامية خلال الفترة، نوصي بالإجراءات التالية لتعزيز الحضور المؤسسي للهيئة ومعالجة الفجوات المرصودة:")).Append("</p>");
        builder.Append("<table data-widths=\"0.5,5,1.7,1\"><tr><th></th><th>التوصية</th><th>الجهة المعنية</th><th>الأولوية</th></tr>");
        var index = 1;
        foreach (RecommendationDto recommendation in recommendations)
        {
            builder.Append("<tr><td>").Append(Number(index++)).Append("</td><td>")
                .Append(Encode(RecommendationText(recommendation))).Append("</td><td>")
                .Append(Encode(recommendation.Responsible)).Append("</td><td>")
                .Append(Encode(recommendation.Priority)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static string RecommendationText(RecommendationDto recommendation)
    {
        var text = new StringBuilder(recommendation.Title);
        if (!string.IsNullOrWhiteSpace(recommendation.Description))
        {
            text.Append(" — ").Append(recommendation.Description);
        }

        var parts = new List<string>();
        AddPart(parts, "المؤشر", recommendation.Kpi);
        AddPart(parts, "الموعد", recommendation.Deadline);
        AddPart(parts, "المتطلبات", recommendation.Dependencies);
        if (parts.Count > 0)
        {
            text.Append(" (").Append(string.Join(" · ", parts)).Append(')');
        }

        return text.ToString();
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

        AppendSubHeading(builder, "تنبيهات تستوجب المتابعة في الفترة القادمة");
        builder.Append("<table data-widths=\"1,1\"><tr><th data-tone=\"mustard\">التنبيه</th>")
            .Append("<th data-tone=\"mustard\">الموقف المقترح</th></tr>");
        foreach (AlertItemDto alert in alerts)
        {
            builder.Append("<tr><td>").Append(Encode(alert.Alert)).Append("</td><td>")
                .Append(Encode(alert.SuggestedPosition)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendSectionSix(StringBuilder builder, FinalMediaReportDetailDto detail)
    {
        AppendSection(builder, 6, "المنهجية والمصادر");
        if (!string.IsNullOrWhiteSpace(detail.Methodology))
        {
            AppendSubHeading(builder, "منهجية الرصد");
            builder.Append("<p>").Append(Encode(detail.Methodology)).Append("</p>");
        }

        AppendQuotesAppendix(builder, detail.QuotesAppendix);
        AppendSources(builder, detail.Sources);
        builder.Append("<div class=\"note\">")
            .Append(Encode("تم إعداد هذا التقرير وفقا للممارسات المعتمدة في الإدارة التنفيذية للتواصل المؤسسي، ويعكس قراءة تحليلية لتغطية الفترة المحددة. الأرقام والنسب الواردة في المؤشرات مبنية على عينة الأخبار المرصودة وقد تختلف عن الأرقام الفعلية لحركة وسائل الإعلام."))
            .Append("</div>");
    }

    private static void AppendQuotesAppendix(StringBuilder builder, IReadOnlyList<QuoteAppendixItemDto> quotes)
    {
        if (quotes.Count == 0)
        {
            return;
        }

        AppendSubHeading(builder, "ملحق التصريحات");
        builder.Append("<table data-widths=\"4.5,1.6,1.5,1.6\"><tr><th>التصريح</th><th>المصدر</th><th>التاريخ</th><th>الموضوع</th></tr>");
        foreach (QuoteAppendixItemDto quote in quotes)
        {
            builder.Append("<tr><td>").Append(Encode(quote.Quote)).Append("</td><td>").Append(Encode(quote.Source))
                .Append("</td><td>").Append(Encode(quote.Date)).Append("</td><td>").Append(Encode(quote.Topic)).Append("</td></tr>");
        }

        builder.Append("</table>");
    }

    private static void AppendSources(StringBuilder builder, IReadOnlyList<SourceRefDto> sources)
    {
        if (sources.Count == 0)
        {
            return;
        }

        AppendSubHeading(builder, "المصادر الرئيسية المعتمدة");
        var index = 1;
        foreach (SourceRefDto source in sources)
        {
            var name = Number(index++) + ". " + source.Name;
            if (!string.IsNullOrWhiteSpace(source.Description))
            {
                name += " — " + source.Description;
            }

            builder.Append("<div class=\"source-item\"><div class=\"source-name\">").Append(Encode(name)).Append("</div>");
            if (!string.IsNullOrWhiteSpace(source.Url))
            {
                builder.Append("<div class=\"source-url\">").Append(Encode(source.Url)).Append("</div>");
            }

            builder.Append("</div>");
        }
    }

    private static void AppendBulletList(StringBuilder builder, IReadOnlyList<string> items)
    {
        builder.Append("<ul>");
        foreach (var item in items)
        {
            builder.Append("<li>").Append(Encode(item)).Append("</li>");
        }

        builder.Append("</ul>");
    }

    private static void AppendSection(StringBuilder builder, int number, string title) =>
        builder.Append("<h2 data-number=\"").Append(Number(number)).Append("\">").Append(Encode(title)).Append("</h2>");

    private static void AppendSubHeading(StringBuilder builder, string title) =>
        builder.Append("<h3>").Append(Encode(title)).Append("</h3>");

    private static string Date(DateTimeOffset value) => value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
