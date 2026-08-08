using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Renders the four-icon grid composition.</summary>
internal static class IconEventGridLayout
{
    internal static string Render(IconEventRenderContext context)
    {
        IconEventParagraphFlow flow = IconEventParagraphFlowBuilder.Build(context.Input.Subtitle, context.Input.ContactEmail, context.Input.ContactPhone);
        var body = RenderBody(context, flow);
        var grid = RenderGrid(context);
        var chips = RenderChips(context, flow);
        return $"<div class=\"poster grid-layout\" style=\"{PosterStyle(context)}\">{Chrome(context)}{body}{grid}{RenderBottom(context, chips)}{Footer(context)}</div>";
    }

    private static string Chrome(IconEventRenderContext context) =>
        IconEventHtmlFragments.RenderDepartment(context.Input.Department, context) + IconEventHtmlFragments.RenderLogo(context.Input.LogoUrl, $"position:absolute;top:{context.Tokens.Margin}px;right:{context.Tokens.Margin}px;z-index:5", context.Tokens.LogoHeight, context.Input.Size);

    private static string Footer(IconEventRenderContext context) =>
        $"<div style=\"position:absolute;bottom:0;left:0;right:0;height:{context.Px(12)}px;background:linear-gradient(90deg,{context.Palette.Accent} 0%,{context.Palette.Secondary} 50%,{context.Palette.Primary} 100%);\"></div>";

    /// <summary>Builds the poster shell for the grid composition.</summary>
    /// <param name="context">The resolved render context.</param>
    /// <returns>The inline style for the poster element.</returns>
    /// <remarks>
    /// The composition is a flex column rather than three absolutely-positioned bands. Absolute
    /// placement worked only while the meta row stayed on one line; a fifth chip wrapped it upward
    /// and it printed straight over the icon tiles.
    /// </remarks>
    private static string PosterStyle(IconEventRenderContext context) =>
        $"width:{context.Width}px;height:{context.Height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('{IconEventVisualAssets.StatsHeroBackgroundDataUri}');background-size:cover;background-position:center;display:flex;flex-direction:column;align-items:center;padding:{HeaderReserve(context)}px {context.Tokens.Margin}px {context.Tokens.Margin + context.Px(24)}px;";

    private static int HeaderReserve(IconEventRenderContext context) =>
        ShowsChrome(context) ? context.Tokens.Margin + context.Tokens.LogoHeight + context.Px(40) : context.Tokens.Margin;

    private static bool ShowsChrome(IconEventRenderContext context) =>
        !IconEventSizeCatalog.SuppressesChrome(context.Input.Size);

    private static string RenderBody(IconEventRenderContext context, IconEventParagraphFlow flow)
    {
        var paragraphs = IconEventHtmlFragments.HasTextBlocks(flow) ? $"<div style=\"max-width:{context.Px(1500)}px;margin:0 auto;display:flex;flex-direction:column;gap:{context.Tokens.ParagraphGap - 4}px;\">{IconEventHtmlFragments.RenderParagraphFlow(flow, ParagraphStyle(context), context.Palette, context.Tokens.MetaFont)}</div>" : string.Empty;
        return $"<div style=\"width:100%;color:#fff;text-align:center;flex:none;\"><h1 style=\"font-size:{context.Px(68)}px;font-weight:900;margin:0 0 {context.Tokens.ParagraphGap}px;line-height:1.15;letter-spacing:-1px;\">{context.Headline}</h1>{paragraphs}</div>";
    }

    private static string RenderBottom(IconEventRenderContext context, string chips) =>
        chips.Length == 0 ? string.Empty : $"<div style=\"width:100%;display:flex;justify-content:center;gap:{context.Px(16)}px;flex-wrap:wrap;flex:none;\">{chips}</div>";

    private static string RenderChips(IconEventRenderContext context, IconEventParagraphFlow flow)
    {
        var contact = RenderContact(context, flow);
        return Meta(context, "calendar", context.Input.Date) + Meta(context, "clock", context.Input.Time) + Meta(context, "map-pin", context.Input.Location) + contact;
    }

    private static string RenderContact(IconEventRenderContext context, IconEventParagraphFlow flow)
    {
        var email = !flow.EmailUsedInline && !string.IsNullOrWhiteSpace(context.Input.ContactEmail) ? IconEventHtmlFragments.RenderContactChip("mail", context.Input.ContactEmail, context.Palette, context.Tokens.MetaFont) : string.Empty;
        var phone = !flow.PhoneUsedInline && !string.IsNullOrWhiteSpace(context.Input.ContactPhone) ? IconEventHtmlFragments.RenderContactChip("phone", context.Input.ContactPhone, context.Palette, context.Tokens.MetaFont) : string.Empty;
        return email + phone;
    }

    private static string RenderGrid(IconEventRenderContext context)
    {
        var icons = context.Input.SupportingIcons.Prepend(context.Input.MainIcon).Take(4).ToList();
        while (icons.Count < 4)
        {
            icons.Add("sparkles");
        }

        return $"<div class=\"grid-plates\" style=\"flex:1 1 auto;min-height:0;display:flex;align-items:center;justify-content:center;padding:{context.Px(24)}px 0;\"><div style=\"display:grid;grid-template-columns:repeat(2,{context.Px(200)}px);gap:{context.Px(32)}px;\">{string.Concat(icons.Select(icon => RenderGridIcon(context, icon)))}</div></div>";
    }

    private static string RenderGridIcon(IconEventRenderContext context, string icon)
    {
        var svg = IconEventIconLibrary.Render(icon, context.Px(110), context.Palette.Accent);
        return $"<div style=\"width:{context.Px(200)}px;height:{context.Px(200)}px;background:rgba(255,255,255,0.12);border:{context.Px(2)}px solid rgba(255,255,255,0.25);border-radius:{context.Px(28)}px;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);\"><div style=\"color:{context.Palette.Accent};\">{svg}</div></div>";
    }

    private static string Meta(IconEventRenderContext context, string icon, string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : IconEventHtmlFragments.RenderMetaChip(icon, value, context.Palette, context.Tokens.MetaFont);

    private static string ParagraphStyle(IconEventRenderContext context) =>
        $"font-size:{context.Tokens.SubtitleSize}px;margin:0;opacity:0.95;font-weight:500;color:#fff;line-height:{context.Tokens.LineHeight};";
}
