using System.Globalization;
using System.Text;
using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Renders the planned copy — lead, list, closing note and meta chips — for any layout.</summary>
/// <remarks>
/// Every composition draws its body through this one path so that a fix to spacing, wrapping or
/// list rendering applies everywhere. Previously each layout assembled its own paragraph flow, and
/// the compositions that were never given a fitting pass simply ran their copy off the canvas.
/// </remarks>
internal static class IconEventPlanFragments
{
    internal static string RenderBody(IconEventRenderContext context, string align)
    {
        IconEventContentPlan plan = context.Plan;
        if (!plan.HasBody)
        {
            return string.Empty;
        }

        var parts = new StringBuilder();
        AppendLead(parts, context, align);
        AppendBullets(parts, context, align);
        AppendClosingNote(parts, context, align);

        var gap = Math.Max(6, context.Tokens.ParagraphGap - 4);
        return $"<div class=\"plan-body\" style=\"display:flex;flex-direction:column;gap:{gap}px;text-align:{align};min-height:0;\">{parts}</div>";
    }

    internal static string RenderMetaChips(IconEventRenderContext context, string justify)
    {
        IReadOnlyList<IconEventMetaChip> chips = context.Plan.MetaChips;
        if (chips.Count == 0)
        {
            return string.Empty;
        }

        var rendered = string.Concat(chips.Select(chip =>
            IconEventHtmlFragments.RenderMetaChip(chip.Icon, chip.Value, context.Palette, context.Tokens.MetaFont)));
        var gap = Math.Max(8, context.Px(16));
        return $"<div class=\"plan-chips\" style=\"display:flex;justify-content:{justify};gap:{gap}px;flex-wrap:wrap;flex:none;\">{rendered}</div>";
    }

    private static void AppendLead(StringBuilder parts, IconEventRenderContext context, string align)
    {
        if (string.IsNullOrWhiteSpace(context.Plan.Lead))
        {
            return;
        }

        var style = $"font-size:{context.Tokens.SubtitleSize}px;margin:0;line-height:{Format(context.Tokens.LineHeight)};font-weight:500;color:#fff;opacity:0.95;text-align:{align};";
        parts.Append(CultureInfo.InvariantCulture, $"<p style=\"{style}\">{IconEventRenderContext.Encode(context.Plan.Lead)}</p>");
    }

    private static void AppendClosingNote(StringBuilder parts, IconEventRenderContext context, string align)
    {
        if (string.IsNullOrWhiteSpace(context.Plan.ClosingNote))
        {
            return;
        }

        var size = Math.Max(14, context.Tokens.SubtitleSize - 4);
        var style = $"font-size:{size}px;margin:0;line-height:1.4;font-weight:500;color:{context.Palette.Accent};text-align:{align};";
        parts.Append(CultureInfo.InvariantCulture, $"<p style=\"{style}\">{IconEventRenderContext.Encode(context.Plan.ClosingNote)}</p>");
    }

    private static void AppendBullets(StringBuilder parts, IconEventRenderContext context, string align)
    {
        IReadOnlyList<IconEventBullet> bullets = context.Plan.Bullets;
        if (bullets.Count == 0)
        {
            return;
        }

        // A list is always read right-aligned in Arabic even inside a centred composition; centring
        // the items themselves makes the ragged edge fall on the reading side.
        var listAlign = align == "center" ? "right" : align;
        var size = Math.Max(14, context.Tokens.SubtitleSize - 4);
        var items = new StringBuilder();
        foreach (IconEventBullet bullet in bullets)
        {
            items.Append(RenderBullet(context, bullet, size, listAlign));
        }

        var gap = Math.Max(6, (int)Math.Round(size * 0.4));
        parts.Append(CultureInfo.InvariantCulture, $"<ul style=\"list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:{gap}px;text-align:{listAlign};\">{items}</ul>");
    }

    private static string RenderBullet(IconEventRenderContext context, IconEventBullet bullet, int size, string listAlign)
    {
        var glyph = Math.Max(12, (int)Math.Round(size * 1.05));
        var svg = IconEventIconLibrary.Render(bullet.Icon, glyph, context.Palette.Accent);
        var gap = Math.Max(8, (int)Math.Round(size * 0.45));
        var nudge = Math.Max(1, (int)Math.Round(size * 0.15));
        return $"<li style=\"display:flex;align-items:flex-start;gap:{gap}px;text-align:{listAlign};direction:rtl;\"><span style=\"flex-shrink:0;margin-top:{nudge}px;color:{context.Palette.Accent};display:inline-flex;\">{svg}</span><span style=\"flex:1;font-size:{size}px;line-height:1.5;font-weight:500;color:#fff;opacity:0.95;\">{IconEventRenderContext.Encode(bullet.Text)}</span></li>";
    }

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
