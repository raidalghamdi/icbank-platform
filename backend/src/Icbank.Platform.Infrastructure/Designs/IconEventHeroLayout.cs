using System.Globalization;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Renders the icon-led hero composition with its source-compatible dense-content mode.</summary>
internal static class IconEventHeroLayout
{
    internal static string Render(IconEventRenderContext context)
    {
        IconEventParagraphFlow flow = IconEventParagraphFlowBuilder.Build(context.Input.Subtitle, context.Input.ContactEmail, context.Input.ContactPhone);
        var dateChips = RenderDateTimeChips(context);
        var contactChips = RenderContactChips(context, flow);
        IconEventHeroMetrics metrics = IconEventHeroMetricsFactory.Create(context, dateChips.Length > 0 || contactChips.Length > 0);
        return metrics.IsVeryDense ? RenderSideLayout(context, flow, metrics) : RenderColumnLayout(context, flow, metrics, dateChips, contactChips);
    }

    private static string RenderColumnLayout(IconEventRenderContext context, IconEventParagraphFlow flow, IconEventHeroMetrics metrics, string dateChips, string contactChips)
    {
        var content = RenderColumnContent(context, flow, metrics);
        var chips = RenderBottomChips(context, dateChips + contactChips);
        return $"<div class=\"poster hero-layout\" style=\"{PosterStyle(context)}\">{RenderChrome(context)}<div class=\"hero-content\" style=\"position:absolute;top:{metrics.HeaderReserve}px;bottom:{metrics.FooterReserve}px;left:{context.Tokens.Margin + 40}px;right:{context.Tokens.Margin + 40}px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:{metrics.IconTextGap}px;\">{content}</div>{chips}{RenderFooter(context, 12)}</div>";
    }

    private static string RenderColumnContent(IconEventRenderContext context, IconEventParagraphFlow flow, IconEventHeroMetrics metrics)
    {
        var icon = RenderCircleIcon(context, metrics.MainIconSize, metrics.IconBoxSize);
        var text = RenderText(context, flow, metrics, "center", "right", metrics.ParagraphGap);
        return icon + text;
    }

    private static string RenderSideLayout(IconEventRenderContext context, IconEventParagraphFlow flow, IconEventHeroMetrics metrics)
    {
        var iconColumn = (int)Math.Round(context.Width * 0.30);
        var iconBox = Math.Min(iconColumn - 80, 480);
        var iconSize = iconBox - 100;
        var iconLeft = (int)Math.Round((iconColumn - iconBox) / 2d);
        var textTop = context.Tokens.Margin + context.Tokens.LogoHeight + 40;
        var icon = RenderSideIcon(context, iconBox, iconSize, iconLeft);
        var text = RenderSideText(context, flow, metrics, textTop, iconColumn);
        return $"<div class=\"poster hero-layout\" style=\"{PosterStyle(context)}\">{RenderChrome(context)}{icon}{text}{RenderFooter(context, 12)}</div>";
    }

    private static string RenderSideIcon(IconEventRenderContext context, int boxSize, int iconSize, int left)
    {
        var icon = IconEventIconLibrary.Render(context.Input.MainIcon, iconSize, context.Palette.Accent);
        return $"<div style=\"position:absolute;top:50%;left:{left}px;transform:translateY(-50%);width:{boxSize}px;height:{boxSize}px;background:rgba(255,255,255,0.10);border:4px solid rgba(255,255,255,0.22);border-radius:50%;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);box-shadow:0 20px 60px rgba(0,0,0,0.2);\"><div style=\"color:{context.Palette.Accent};\">{icon}</div></div>";
    }

    private static string RenderSideText(IconEventRenderContext context, IconEventParagraphFlow flow, IconEventHeroMetrics metrics, int top, int iconColumn)
    {
        var text = RenderText(context, flow, metrics, "right", "right", Math.Max(context.Tokens.ParagraphGap - 8, 14));
        return $"<div style=\"position:absolute;top:{top}px;right:{context.Tokens.Margin + 20}px;left:{iconColumn + 20}px;bottom:{context.Tokens.Margin + 40}px;display:flex;flex-direction:column;justify-content:center;text-align:right;\">{text}</div>";
    }

    private static string RenderText(IconEventRenderContext context, IconEventParagraphFlow flow, IconEventHeroMetrics metrics, string titleAlign, string paragraphAlign, int gap)
    {
        var paragraphStyle = $"font-size:{metrics.SubtitleSize}px;margin:0;opacity:0.95;font-weight:500;line-height:{(context.Tokens.LineHeight - 0.1).ToString(CultureInfo.InvariantCulture)};text-align:{paragraphAlign};";
        var paragraphs = IconEventHtmlFragments.HasTextBlocks(flow) ? $"<div class=\"hero-paragraphs\" style=\"width:100%;display:flex;flex-direction:column;gap:{gap}px;text-align:{paragraphAlign};\">{IconEventHtmlFragments.RenderParagraphFlow(flow, paragraphStyle, context.Palette, context.Tokens.MetaFont, paragraphAlign, metrics.IsVeryDense ? 36 : null, metrics.IsVeryDense ? 27 : metrics.SubtitleSize)}</div>" : string.Empty;
        return $"<div class=\"hero-text\" style=\"width:100%;max-width:{metrics.SubtitleMaxWidth}px;display:flex;flex-direction:column;align-items:center;\"><h1 class=\"hero-title\" style=\"font-size:{metrics.TitleSize}px;font-weight:900;margin:0 0 {metrics.TitleGap}px;line-height:1.15;letter-spacing:-1px;text-align:{titleAlign};\">{context.Headline}</h1>{paragraphs}</div>";
    }

    private static string RenderBottomChips(IconEventRenderContext context, string chips)
    {
        return chips.Length == 0 ? string.Empty : $"<div style=\"position:absolute;bottom:6%;left:0;right:0;display:flex;justify-content:center;gap:20px;flex-wrap:wrap;padding:0 {context.Tokens.Margin}px;\">{chips}</div>";
    }

    private static string RenderChrome(IconEventRenderContext context) =>
        IconEventHtmlFragments.RenderDepartment(context.Input.Department, context) + IconEventHtmlFragments.RenderLogo(context.Input.LogoUrl, $"position:absolute;top:{context.Tokens.Margin}px;right:{context.Tokens.Margin}px;z-index:5", context.Tokens.LogoHeight, context.Input.Size);

    private static string RenderCircleIcon(IconEventRenderContext context, int iconSize, int boxSize)
    {
        var icon = IconEventIconLibrary.Render(context.Input.MainIcon, iconSize, context.Palette.Accent);
        return $"<div style=\"flex-shrink:0;width:{boxSize}px;height:{boxSize}px;background:rgba(255,255,255,0.10);border:4px solid rgba(255,255,255,0.22);border-radius:50%;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);box-shadow:0 20px 60px rgba(0,0,0,0.2);\"><div style=\"color:{context.Palette.Accent};\">{icon}</div></div>";
    }

    private static string RenderContactChips(IconEventRenderContext context, IconEventParagraphFlow flow)
    {
        var email = !flow.EmailUsedInline && !string.IsNullOrWhiteSpace(context.Input.ContactEmail) ? IconEventHtmlFragments.RenderContactChip("mail", context.Input.ContactEmail, context.Palette, context.Tokens.MetaFont) : string.Empty;
        var phone = !flow.PhoneUsedInline && !string.IsNullOrWhiteSpace(context.Input.ContactPhone) ? IconEventHtmlFragments.RenderContactChip("phone", context.Input.ContactPhone, context.Palette, context.Tokens.MetaFont) : string.Empty;
        return email + phone;
    }

    private static string RenderDateTimeChips(IconEventRenderContext context) =>
        RenderMeta(context, "calendar", context.Input.Date) + RenderMeta(context, "clock", context.Input.Time) + RenderMeta(context, "map-pin", context.Input.Location);

    private static string RenderFooter(IconEventRenderContext context, int height) =>
        $"<div style=\"position:absolute;bottom:0;left:0;right:0;height:{height}px;background:linear-gradient(90deg,{context.Palette.Accent} 0%,{context.Palette.Secondary} 50%,{context.Palette.Primary} 100%);\"></div>";

    private static string RenderMeta(IconEventRenderContext context, string icon, string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : IconEventHtmlFragments.RenderMetaChip(icon, value, context.Palette, context.Tokens.MetaFont);

    private static string PosterStyle(IconEventRenderContext context) =>
        $"width:{context.Width}px;height:{context.Height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('{IconEventVisualAssets.StatsHeroBackgroundDataUri}');background-size:cover;background-position:center;";
}
