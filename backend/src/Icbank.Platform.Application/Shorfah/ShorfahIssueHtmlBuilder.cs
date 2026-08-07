using System.Net;
using System.Text;
using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Builds the shared HTML representation of an issue used by both export endpoints
/// (<c>GET /shorfah/issues/:id/pdf</c> and <c>GET /shorfah/issues/:id/pdf.pdf</c>), ported from
/// <c>buildShorfahPdfHtml()</c> in <c>shorfah-pdf.ts</c> (BUSINESS-RULES.md §1.9). Every value
/// that originates from the database (including AI-generated <c>ContentMd</c>) is passed through
/// <see cref="WebUtility.HtmlEncode(string?)"/> before being placed into the document -- never via
/// raw string interpolation of untrusted content. This closes the H-1 class of defect
/// BUSINESS-RULES.md §1.9 flags for the Node source's <c>shorfah-pdf.ts</c>: <c>mdToHtml()</c>
/// escaped correctly, but "other interpolation sites ... do not" (DEFECT-LOG.md SEC-04) -- every
/// interpolation site in this builder is encoded, closing SEC-04 for the .NET port. Section media
/// image inlining (base64 <c>data:</c> URLs) is deferred, see WAVE4A-PORT-NOTES.md -- media
/// belongs to wave 4b's scope per the task's explicit boundary.
/// </summary>
public static class ShorfahIssueHtmlBuilder
{
    private const string Styles = "<style>" +
        "body{font-family:'Frutiger LT Arabic','Arial',sans-serif;direction:rtl;margin:40px;color:#1a1a1a;}" +
        "h1{color:#0069A7;border-bottom:2px solid #0069A7;padding-bottom:8px;}" +
        "h2{color:#0069A7;margin-top:24px;}" +
        ".meta{color:#888888;font-size:14px;margin-bottom:16px;}" +
        ".section-desc{color:#666666;font-style:italic;margin-bottom:12px;}" +
        "</style>";

    /// <summary>Builds the full issue HTML document.</summary>
    /// <param name="issue">The issue to render.</param>
    /// <param name="sections">The sections to render, already filtered by <see cref="ShorfahExportSectionSelector"/> and ordered by display order.</param>
    /// <returns>The fully HTML-encoded document.</returns>
    public static string Build(ShorfahIssue issue, IReadOnlyList<ShorfahSection> sections)
    {
        var builder = new StringBuilder();
        builder.Append("<!DOCTYPE html><html dir=\"rtl\" lang=\"ar\"><head><meta charset=\"UTF-8\">").Append(Styles).Append("</head><body>");
        AppendHeader(builder, issue);
        if (!string.IsNullOrWhiteSpace(issue.EditorLetter))
        {
            builder.Append("<h2>رسالة رئيس التحرير</h2><p>").Append(Encode(issue.EditorLetter)).Append("</p>");
        }

        foreach (ShorfahSection section in sections)
        {
            AppendSection(builder, section);
        }

        builder.Append("</body></html>");
        return builder.ToString();
    }

    private static void AppendHeader(StringBuilder builder, ShorfahIssue issue)
    {
        builder.Append("<h1>").Append(Encode(issue.TitleAr)).Append("</h1>");
        if (!string.IsNullOrWhiteSpace(issue.SubtitleAr))
        {
            builder.Append("<p>").Append(Encode(issue.SubtitleAr)).Append("</p>");
        }

        var monthName = ArabicMonthNames.For(issue.Month);
        builder.Append("<p class=\"meta\">العدد ").Append(issue.IssueNo).Append(" · ").Append(Encode(monthName)).Append(' ').Append(issue.Year).Append("</p>");
    }

    private static void AppendSection(StringBuilder builder, ShorfahSection section)
    {
        builder.Append("<h2>").Append(Encode(section.TitleAr)).Append("</h2>");
        if (!string.IsNullOrWhiteSpace(section.DescriptionAr))
        {
            builder.Append("<p class=\"section-desc\">").Append(Encode(section.DescriptionAr)).Append("</p>");
        }

        if (!string.IsNullOrWhiteSpace(section.ContentMd))
        {
            builder.Append("<p>").Append(Encode(section.ContentMd)).Append("</p>");
        }
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
