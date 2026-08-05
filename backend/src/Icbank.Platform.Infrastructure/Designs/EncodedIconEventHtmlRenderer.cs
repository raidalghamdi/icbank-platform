using System.Net;
using System.Text;
using Icbank.Platform.Application.Designs.IconEvent;
using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>
/// Default <see cref="IIconEventHtmlRenderer"/> implementation. Emits a minimal but complete,
/// fully HTML-encoded poster document reflecting the resolved <see cref="IconEventInput"/>
/// (headline/subtitle/department/hashtag/contact/stats/layout/size) rather than the Node source's
/// full pixel-perfect CSS layout engine (deferred, see WAVE3B-PORT-NOTES.md). Every value that
/// could have originated from AI extraction or client input is passed through
/// <see cref="WebUtility.HtmlEncode(string?)"/> before being placed into the document -- never via
/// raw string interpolation of untrusted content, matching the same pattern as
/// <c>FinalReportHtmlBuilder</c> and closing SEC-12/H-1 for this render path.
/// </summary>
public sealed class EncodedIconEventHtmlRenderer : IIconEventHtmlRenderer
{
    /// <inheritdoc />
    public string Render(IconEventInput input)
    {
        (var width, var height) = IconEventSizeCatalog.Resolve(input.Size);
        var builder = new StringBuilder();
        builder.Append("<!DOCTYPE html><html dir=\"rtl\" lang=\"ar\"><head><meta charset=\"UTF-8\">");
        builder.Append("<style>body{margin:0;font-family:Arial,sans-serif;background:#0f4c56;color:#fff;}")
            .Append(".poster{width:").Append(width).Append("px;height:").Append(height).Append("px;padding:48px;box-sizing:border-box;}")
            .Append("</style></head><body><div class=\"poster\" data-autofit=\"true\">");

        builder.Append("<h1>").Append(Encode(input.Headline)).Append("</h1>");
        if (!string.IsNullOrWhiteSpace(input.Subtitle))
        {
            builder.Append("<p>").Append(Encode(input.Subtitle)).Append("</p>");
        }

        AppendMeta(builder, input);
        AppendStats(builder, input.Stats);
        builder.Append("</div></body></html>");
        return builder.ToString();
    }

    private static void AppendMeta(StringBuilder builder, IconEventInput input)
    {
        builder.Append("<div class=\"meta\">");
        AppendIfPresent(builder, input.Department);
        AppendIfPresent(builder, input.Hashtag);
        AppendIfPresent(builder, input.Date);
        AppendIfPresent(builder, input.Time);
        AppendIfPresent(builder, input.Location);
        AppendIfPresent(builder, input.ContactEmail);
        AppendIfPresent(builder, input.ContactPhone);
        builder.Append("</div>");
    }

    private static void AppendIfPresent(StringBuilder builder, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.Append("<span>").Append(Encode(value)).Append("</span>");
        }
    }

    private static void AppendStats(StringBuilder builder, IReadOnlyList<IconEventStat> stats)
    {
        if (stats.Count == 0)
        {
            return;
        }

        builder.Append("<ul class=\"stats\">");
        foreach (IconEventStat stat in stats)
        {
            builder.Append("<li><strong>").Append(Encode(stat.Value)).Append("</strong> ").Append(Encode(stat.Label)).Append("</li>");
        }

        builder.Append("</ul>");
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
