using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Renders the four-icon grid composition.</summary>
internal static class IconEventGridLayout
{
    internal static string Render(IconEventRenderContext context)
    {
        var body = RenderBody(context);
        var grid = RenderGrid(context);
        var chips = IconEventPlanFragments.RenderMetaChips(context, "center");
        return $"<div class=\"poster grid-layout\" data-fit-mode=\"grow\" style=\"{PosterStyle(context)}\">{Chrome(context)}{body}{grid}{chips}{Footer(context)}</div>";
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

    private static string RenderBody(IconEventRenderContext context)
    {
        var body = IconEventPlanFragments.RenderBody(context, "center", withList: false);
        var paragraphs = body.Length == 0 ? string.Empty : $"<div style=\"width:84%;margin:0 auto;\">{body}</div>";
        return $"<div style=\"width:100%;color:#fff;text-align:center;flex:none;\"><h1 style=\"font-size:{context.Px(68)}px;font-weight:900;margin:0 0 {context.Tokens.ParagraphGap}px;line-height:1.15;letter-spacing:-1px;\">{context.Headline}</h1>{paragraphs}</div>";
    }

    private static string RenderGrid(IconEventRenderContext context)
    {
        // The plan already resolved three distinct supporting icons against the copy, so the grid
        // never has to pad itself with a decorative placeholder.
        List<string> icons = context.Plan.Bullets.Count > 0
            ? context.Plan.Bullets.Select(bullet => bullet.Icon).Take(4).ToList()
            : context.Plan.SupportingIcons.Prepend(context.Plan.MainIcon).Take(4).ToList();

        return $"<div class=\"grid-plates\" style=\"flex:0 0 auto;display:flex;align-items:center;justify-content:center;padding:{context.Px(24)}px 0;margin:auto 0;\"><div style=\"display:grid;font-size:{context.Px(200)}px;grid-template-columns:repeat(2,1em);gap:0.16em;\">{string.Concat(icons.Select(icon => RenderGridIcon(context, icon)))}</div></div>";
    }

    private static string RenderGridIcon(IconEventRenderContext context, string icon)
    {
        var svg = IconEventIconLibrary.Render(icon, context.Px(110), context.Palette.Accent);
        return $"<div style=\"width:1em;height:1em;background:rgba(255,255,255,0.12);border:{context.Px(2)}px solid rgba(255,255,255,0.25);border-radius:0.14em;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);\"><div style=\"color:{context.Palette.Accent};\">{svg}</div></div>";
    }
}
