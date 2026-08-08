namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Renders the text-only typography composition.</summary>
internal static class IconEventTypographyLayout
{
    internal static string Render(IconEventRenderContext context)
    {
        var paragraphs = RenderParagraphs(context);
        var chips = IconEventPlanFragments.RenderMetaChips(context, "center");
        return $"<div class=\"poster typography-layout\" style=\"{PosterStyle(context)}\">{Chrome(context)}<div style=\"position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);width:100%;padding:0 {context.Px(160)}px;text-align:center;\"><h1 style=\"font-size:{context.Px(96)}px;font-weight:900;margin:0 0 {context.Tokens.ParagraphGap + 16}px;line-height:1.2;letter-spacing:-1px;color:#fff;\">{context.Headline}</h1>{paragraphs}{chips}</div>{Footer(context)}</div>";
    }

    private static string Chrome(IconEventRenderContext context) =>
        IconEventHtmlFragments.RenderDepartment(context.Input.Department, context) + IconEventHtmlFragments.RenderLogo(context.Input.LogoUrl, $"position:absolute;top:{context.Tokens.Margin}px;right:{context.Tokens.Margin}px;z-index:10", context.Tokens.LogoHeight, context.Input.Size);

    private static string Footer(IconEventRenderContext context) =>
        $"<div style=\"position:absolute;bottom:0;left:0;right:0;height:{context.Px(14)}px;background:linear-gradient(90deg,{context.Palette.Accent} 0%,{context.Palette.Secondary} 50%,{context.Palette.Primary} 100%);\"></div>";

    private static string PosterStyle(IconEventRenderContext context) =>
        $"width:{context.Width}px;height:{context.Height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('{IconEventVisualAssets.StatsHeroBackgroundDataUri}');background-size:cover;background-position:center;";

    private static string RenderParagraphs(IconEventRenderContext context)
    {
        var body = IconEventPlanFragments.RenderBody(context, "center");
        return body.Length == 0
            ? string.Empty
            : $"<div style=\"max-width:{context.Px(1600)}px;margin:0 auto {context.Tokens.ParagraphGap + 20}px;\">{body}</div>";
    }
}
