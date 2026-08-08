namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Renders the icon-led hero composition with its source-compatible dense-content mode.</summary>
internal static class IconEventHeroLayout
{
    internal static string Render(IconEventRenderContext context)
    {
        var chips = IconEventPlanFragments.RenderMetaChips(context, "center");
        IconEventHeroMetrics metrics = IconEventHeroMetricsFactory.Create(context, chips.Length > 0);
        return metrics.IsVeryDense ? RenderSideLayout(context, metrics) : RenderColumnLayout(context, metrics, chips);
    }

    private static string RenderColumnLayout(IconEventRenderContext context, IconEventHeroMetrics metrics, string metaChips)
    {
        var content = RenderColumnContent(context, metrics);
        var chips = RenderBottomChips(context, metaChips);
        return $"<div class=\"poster hero-layout\" style=\"{PosterStyle(context)}\">{RenderChrome(context)}<div class=\"hero-content\" style=\"position:absolute;top:{metrics.HeaderReserve}px;bottom:{metrics.FooterReserve}px;left:{context.Tokens.Margin + 40}px;right:{context.Tokens.Margin + 40}px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:{metrics.IconTextGap}px;\">{content}</div>{chips}{RenderFooter(context, 12)}</div>";
    }

    private static string RenderColumnContent(IconEventRenderContext context, IconEventHeroMetrics metrics)
    {
        var icon = RenderCircleIcon(context, metrics.MainIconSize, metrics.IconBoxSize);
        var text = RenderText(context, metrics, "center", "right");
        return icon + text;
    }

    private static string RenderSideLayout(IconEventRenderContext context, IconEventHeroMetrics metrics)
    {
        var iconColumn = (int)Math.Round(context.Width * 0.30);
        var iconBox = Math.Min(iconColumn - 80, 480);
        var iconSize = iconBox - 100;
        var iconLeft = (int)Math.Round((iconColumn - iconBox) / 2d);
        var textTop = context.Tokens.Margin + context.Tokens.LogoHeight + 40;
        var icon = RenderSideIcon(context, iconBox, iconSize, iconLeft);
        var text = RenderSideText(context, metrics, textTop, iconColumn);
        return $"<div class=\"poster hero-layout\" style=\"{PosterStyle(context)}\">{RenderChrome(context)}{icon}{text}{RenderFooter(context, 12)}</div>";
    }

    private static string RenderSideIcon(IconEventRenderContext context, int boxSize, int iconSize, int left)
    {
        var icon = IconEventIconLibrary.Render(context.Plan.MainIcon, iconSize, context.Palette.Accent);
        return $"<div style=\"position:absolute;top:50%;left:{left}px;transform:translateY(-50%);width:{boxSize}px;height:{boxSize}px;background:rgba(255,255,255,0.10);border:4px solid rgba(255,255,255,0.22);border-radius:50%;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);box-shadow:0 20px 60px rgba(0,0,0,0.2);\"><div style=\"color:{context.Palette.Accent};\">{icon}</div></div>";
    }

    private static string RenderSideText(IconEventRenderContext context, IconEventHeroMetrics metrics, int top, int iconColumn)
    {
        var text = RenderText(context, metrics, "right", "right");
        return $"<div style=\"position:absolute;top:{top}px;right:{context.Tokens.Margin + 20}px;left:{iconColumn + 20}px;bottom:{context.Tokens.Margin + 40}px;display:flex;flex-direction:column;justify-content:center;text-align:right;\">{text}</div>";
    }

    private static string RenderText(IconEventRenderContext context, IconEventHeroMetrics metrics, string titleAlign, string paragraphAlign)
    {
        var body = IconEventPlanFragments.RenderBody(context, paragraphAlign);
        return $"<div class=\"hero-text\" style=\"width:100%;max-width:{metrics.SubtitleMaxWidth}px;display:flex;flex-direction:column;align-items:center;min-height:0;\"><h1 class=\"hero-title\" style=\"font-size:{metrics.TitleSize}px;font-weight:900;margin:0 0 {metrics.TitleGap}px;line-height:1.15;letter-spacing:-1px;text-align:{titleAlign};\">{context.Headline}</h1>{body}</div>";
    }

    private static string RenderBottomChips(IconEventRenderContext context, string chips)
    {
        return chips.Length == 0 ? string.Empty : $"<div style=\"position:absolute;bottom:6%;left:0;right:0;display:flex;justify-content:center;gap:20px;flex-wrap:wrap;padding:0 {context.Tokens.Margin}px;\">{chips}</div>";
    }

    private static string RenderChrome(IconEventRenderContext context) =>
        IconEventHtmlFragments.RenderDepartment(context.Input.Department, context) + IconEventHtmlFragments.RenderLogo(context.Input.LogoUrl, $"position:absolute;top:{context.Tokens.Margin}px;right:{context.Tokens.Margin}px;z-index:5", context.Tokens.LogoHeight, context.Input.Size);

    private static string RenderCircleIcon(IconEventRenderContext context, int iconSize, int boxSize)
    {
        var icon = IconEventIconLibrary.Render(context.Plan.MainIcon, iconSize, context.Palette.Accent);
        return $"<div style=\"flex-shrink:0;width:{boxSize}px;height:{boxSize}px;background:rgba(255,255,255,0.10);border:4px solid rgba(255,255,255,0.22);border-radius:50%;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);box-shadow:0 20px 60px rgba(0,0,0,0.2);\"><div style=\"color:{context.Palette.Accent};\">{icon}</div></div>";
    }

    private static string RenderFooter(IconEventRenderContext context, int height) =>
        $"<div style=\"position:absolute;bottom:0;left:0;right:0;height:{height}px;background:linear-gradient(90deg,{context.Palette.Accent} 0%,{context.Palette.Secondary} 50%,{context.Palette.Primary} 100%);\"></div>";

    private static string PosterStyle(IconEventRenderContext context) =>
        $"width:{context.Width}px;height:{context.Height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('{IconEventVisualAssets.StatsHeroBackgroundDataUri}');background-size:cover;background-position:center;";
}
