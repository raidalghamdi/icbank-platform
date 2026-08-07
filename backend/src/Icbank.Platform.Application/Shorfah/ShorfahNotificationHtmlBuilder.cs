using System.Net;
using System.Text;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Builds the HTML email bodies for Shorfah notifications (BUSINESS-RULES.md §1.7:
/// <c>buildInitialEmailHtml</c>/<c>buildPublishEmailHtml</c> in <c>lib/notify.ts</c>). Every
/// value that originates from the database is passed through <see cref="WebUtility.HtmlEncode(string?)"/>
/// before being placed into the document -- never via raw string interpolation of untrusted
/// content, closing the same H-1 class of defect <c>FinalReportHtmlBuilder</c> closes for Wave 3a
/// and the general Shorfah-render-pipeline concern BUSINESS-RULES.md §1.9 flags for
/// <c>shorfah-pdf.ts</c>'s other, unescaped interpolation sites.
/// </summary>
public static class ShorfahNotificationHtmlBuilder
{
    /// <summary>Builds the "initial contribution request" email body.</summary>
    /// <param name="recipientName">The recipient's display name.</param>
    /// <param name="sectionTitleAr">The section's Arabic title.</param>
    /// <param name="issueTitleAr">The issue's Arabic title.</param>
    /// <param name="deadline">The formatted SLA deadline.</param>
    /// <param name="url">The absolute in-app URL to the issue.</param>
    /// <returns>The fully HTML-encoded email body.</returns>
    public static string BuildInitial(string recipientName, string sectionTitleAr, string issueTitleAr, string deadline, string url)
    {
        var builder = new StringBuilder();
        builder.Append("<!DOCTYPE html><html dir=\"rtl\" lang=\"ar\"><body>");
        builder.Append("<p>مرحباً ").Append(Encode(recipientName)).Append("،</p>");
        builder.Append("<p>تمت دعوتك للمساهمة في قسم \"").Append(Encode(sectionTitleAr))
            .Append("\" من عدد \"").Append(Encode(issueTitleAr)).Append("\".</p>");
        builder.Append("<p>آخر موعد: <strong>").Append(Encode(deadline)).Append("</strong></p>");
        builder.Append("<p><a href=\"").Append(Encode(url)).Append("\">فتح العدد</a></p>");
        builder.Append("</body></html>");
        return builder.ToString();
    }

    /// <summary>Builds the "issue published" email body.</summary>
    /// <param name="issueTitleAr">The issue's Arabic title.</param>
    /// <param name="monthNameAr">The Arabic month name.</param>
    /// <param name="year">The calendar year.</param>
    /// <param name="issueNo">The issue number.</param>
    /// <param name="url">The absolute in-app URL to the issue.</param>
    /// <param name="pdfUrl">The absolute PDF download URL.</param>
    /// <returns>The fully HTML-encoded email body.</returns>
    public static string BuildPublished(string issueTitleAr, string monthNameAr, int year, int issueNo, string url, string pdfUrl)
    {
        var builder = new StringBuilder();
        builder.Append("<!DOCTYPE html><html dir=\"rtl\" lang=\"ar\"><body>");
        builder.Append("<p>عدد جديد من شُرفة متوفر الآن: <strong>").Append(Encode(issueTitleAr)).Append("</strong></p>");
        builder.Append("<p>العدد ").Append(issueNo).Append(" — ").Append(Encode(monthNameAr)).Append(' ').Append(year).Append("</p>");
        builder.Append("<p><a href=\"").Append(Encode(url)).Append("\">قراءة العدد</a> · ");
        builder.Append("<a href=\"").Append(Encode(pdfUrl)).Append("\">تحميل PDF</a></p>");
        builder.Append("</body></html>");
        return builder.ToString();
    }

    /// <summary>Builds the overdue-reminder email body (BUSINESS-RULES.md §1.6/§1.7, <c>buildOverdueEmailHtml</c>).</summary>
    /// <param name="recipientName">The recipient's display name.</param>
    /// <param name="sectionTitleAr">The section's Arabic title.</param>
    /// <param name="issueTitleAr">The issue's Arabic title.</param>
    /// <param name="daysOverdue">The number of days past the SLA deadline.</param>
    /// <param name="url">The absolute in-app URL to the issue.</param>
    /// <returns>The fully HTML-encoded email body.</returns>
    public static string BuildOverdue(string recipientName, string sectionTitleAr, string issueTitleAr, int daysOverdue, string url)
    {
        var builder = new StringBuilder();
        builder.Append("<!DOCTYPE html><html dir=\"rtl\" lang=\"ar\"><body>");
        builder.Append("<p>عزيزي ").Append(Encode(recipientName)).Append("،</p>");
        builder.Append("<p>قسم \"").Append(Encode(sectionTitleAr)).Append("\" في عدد \"")
            .Append(Encode(issueTitleAr)).Append("\" تأخر بمقدار ").Append(daysOverdue).Append(" يوم عن الموعد المحدد.</p>");
        builder.Append("<p>يُرجى تسليم المحتوى في أقرب وقت ممكن.</p>");
        builder.Append("<p><a href=\"").Append(Encode(url)).Append("\">فتح العدد</a></p>");
        builder.Append("</body></html>");
        return builder.ToString();
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
