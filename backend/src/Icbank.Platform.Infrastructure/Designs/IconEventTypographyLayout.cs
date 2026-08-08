namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Renders the text-only typography composition.</summary>
internal static class IconEventTypographyLayout
{
    internal static string Render(IconEventRenderContext context)
    {
        IconEventParagraphFlow flow = IconEventParagraphFlowBuilder.Build(context.Input.Subtitle, context.Input.ContactEmail, context.Input.ContactPhone);
        var paragraphs = RenderParagraphs(context, flow);
        var chips = RenderChips(context, flow);
        return $"<div class=\"poster typography-layout\" style=\"{PosterStyle(context)}\">{Chrome(context)}<div style=\"position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);width:100%;padding:0 {context.Px(160)}px;text-align:center;\"><h1 style=\"font-size:{context.Px(96)}px;font-weight:900;margin:0 0 {context.Tokens.ParagraphGap + 16}px;line-height:1.2;letter-spacing:-1px;color:#fff;\">{context.Headline}</h1>{paragraphs}{(chips.Length > 0 ? $"<div style=\"display:flex;justify-content:center;gap:{context.Px(18)}px;flex-wrap:wrap;\">{chips}</div>" : string.Empty)}</div>{Footer(context)}</div>";
    }

    private static string Chrome(IconEventRenderContext context) =>
        IconEventHtmlFragments.RenderDepartment(context.Input.Department, context) + IconEventHtmlFragments.RenderLogo(context.Input.LogoUrl, $"position:absolute;top:{context.Tokens.Margin}px;right:{context.Tokens.Margin}px;z-index:10", context.Tokens.LogoHeight, context.Input.Size);

    private static string Footer(IconEventRenderContext context) =>
        $"<div style=\"position:absolute;bottom:0;left:0;right:0;height:{context.Px(14)}px;background:linear-gradient(90deg,{context.Palette.Accent} 0%,{context.Palette.Secondary} 50%,{context.Palette.Primary} 100%);\"></div>";

    private static string PosterStyle(IconEventRenderContext context) =>
        $"width:{context.Width}px;height:{context.Height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('{IconEventVisualAssets.StatsHeroBackgroundDataUri}');background-size:cover;background-position:center;";

    private static string RenderChips(IconEventRenderContext context, IconEventParagraphFlow flow)
    {
        var email = !flow.EmailUsedInline && !string.IsNullOrWhiteSpace(context.Input.ContactEmail) ? IconEventHtmlFragments.RenderContactChip("mail", context.Input.ContactEmail, context.Palette, context.Tokens.MetaFont) : string.Empty;
        var phone = !flow.PhoneUsedInline && !string.IsNullOrWhiteSpace(context.Input.ContactPhone) ? IconEventHtmlFragments.RenderContactChip("phone", context.Input.ContactPhone, context.Palette, context.Tokens.MetaFont) : string.Empty;
        return Meta(context, "calendar", context.Input.Date) + Meta(context, "clock", context.Input.Time) + Meta(context, "map-pin", context.Input.Location) + email + phone;
    }

    private static string RenderParagraphs(IconEventRenderContext context, IconEventParagraphFlow flow)
    {
        if (!IconEventHtmlFragments.HasTextBlocks(flow))
        {
            return string.Empty;
        }

        var style = $"font-size:{context.Tokens.SubtitleSize + 4}px;margin:0;line-height:{context.Tokens.LineHeight};font-weight:500;color:#fff;opacity:0.95;";
        return $"<div style=\"max-width:{context.Px(1600)}px;margin:0 auto {context.Tokens.ParagraphGap + 20}px;display:flex;flex-direction:column;gap:{context.Tokens.ParagraphGap}px;\">{IconEventHtmlFragments.RenderParagraphFlow(flow, style, context.Palette, context.Tokens.MetaFont)}</div>";
    }

    private static string Meta(IconEventRenderContext context, string icon, string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : IconEventHtmlFragments.RenderMetaChip(icon, value, context.Palette, context.Tokens.MetaFont);
}
