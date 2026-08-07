using System.Globalization;
using System.Text;
using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Assembles the plain-text document body for the DOCX export, ported from the section-assembly
/// portion of <c>GET /shorfah/issues/:id/docx</c> in <c>shorfah.ts:1094-1264</c> (BUSINESS-RULES.md
/// §1.9). Markdown is stripped via <see cref="MarkdownStripper"/>, matching the Node source's
/// fidelity-losing (but deliberate) plain-text-only approach.
/// </summary>
public static class ShorfahIssuePlainTextBuilder
{
    /// <summary>Builds the full plain-text document body for an issue.</summary>
    /// <param name="issue">The issue being exported.</param>
    /// <param name="sections">The sections to include, already filtered by <see cref="ShorfahExportSectionSelector"/> and ordered by display order.</param>
    /// <returns>The assembled plain-text body.</returns>
    public static string Build(ShorfahIssue issue, IReadOnlyList<ShorfahSection> sections)
    {
        var builder = new StringBuilder();
        AppendTitlePage(builder, issue);
        if (!string.IsNullOrWhiteSpace(issue.EditorLetter))
        {
            builder.Append("رسالة رئيس التحرير\n\n").Append(MarkdownStripper.Strip(issue.EditorLetter)).Append("\n\n");
        }

        foreach (ShorfahSection section in sections)
        {
            AppendSection(builder, section);
        }

        var monthName = ArabicMonthNames.For(issue.Month);
        builder.Append("· · ·\n").Append(CultureInfo.InvariantCulture, $"شُرفة · العدد {issue.IssueNo} · {monthName} {issue.Year}");
        return builder.ToString();
    }

    private static void AppendTitlePage(StringBuilder builder, ShorfahIssue issue)
    {
        builder.Append(issue.TitleAr).Append('\n');
        if (!string.IsNullOrWhiteSpace(issue.SubtitleAr))
        {
            builder.Append(issue.SubtitleAr).Append('\n');
        }

        var monthName = ArabicMonthNames.For(issue.Month);
        builder.Append(CultureInfo.InvariantCulture, $"العدد {issue.IssueNo} · {monthName} {issue.Year}").Append("\n\n");
    }

    private static void AppendSection(StringBuilder builder, ShorfahSection section)
    {
        builder.Append(section.TitleAr).Append('\n');
        if (!string.IsNullOrWhiteSpace(section.DescriptionAr))
        {
            builder.Append(section.DescriptionAr).Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(section.ContentMd))
        {
            builder.Append(MarkdownStripper.Strip(section.ContentMd));
        }

        builder.Append("\n\n");
    }
}
