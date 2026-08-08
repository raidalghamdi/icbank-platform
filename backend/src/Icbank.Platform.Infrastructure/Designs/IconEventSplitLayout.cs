using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Renders the landscape-family icon-and-copy split composition.</summary>
internal static class IconEventSplitLayout
{
    internal static string Render(IconEventRenderContext context)
    {
        var reserve = context.Tokens.Margin + (context.Tokens.DeptPaddingV * 2) + context.Tokens.DeptFont + 60;
        return $"<div class=\"poster split-layout\" data-fit-mode=\"grow\" style=\"{PosterStyle(context)}display:flex;\">{Chrome(context)}{RenderIconColumn(context, reserve)}{RenderTextColumn(context, reserve)}{Footer(context)}</div>";
    }

    private static string Chrome(IconEventRenderContext context) =>
        IconEventHtmlFragments.RenderDepartment(context.Input.Department, context) + IconEventHtmlFragments.RenderLogo(context.Input.LogoUrl, $"position:absolute;top:{context.Tokens.Margin}px;right:{context.Tokens.Margin}px;z-index:10", context.Tokens.LogoHeight, context.Input.Size);

    private static string Footer(IconEventRenderContext context) =>
        $"<div style=\"position:absolute;bottom:0;left:0;right:0;height:{context.Px(12)}px;background:linear-gradient(90deg,{context.Palette.Accent} 0%,{context.Palette.Secondary} 50%,{context.Palette.Primary} 100%);\"></div>";

    private static string PosterStyle(IconEventRenderContext context) =>
        $"width:{context.Width}px;height:{context.Height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('{IconEventVisualAssets.StatsHeroBackgroundDataUri}');background-size:cover;background-position:center;";

    /// <summary>Gets the share of the canvas width given to the copy.</summary>
    /// <param name="context">The render context.</param>
    /// <returns>A percentage of the poster width.</returns>
    /// <remarks>
    /// A fixed split spent the same 40% on a single glyph whether the poster carried one sentence
    /// or a four-item checklist, which forced a long list into a narrow column where every item
    /// wrapped onto two lines.
    /// </remarks>
    private static int TextWidth(IconEventRenderContext context) =>
        IconEventSizeCatalog.SuppressesChrome(context.Input.Size)
            ? 70
            : context.Plan.Bullets.Count >= 3 ? 72 : 62;

    private static string RenderIconColumn(IconEventRenderContext context, int reserve)
    {
        var icon = IconEventIconLibrary.Render(context.Plan.MainIcon, context.Px(240), context.Palette.Accent);
        return $"<div style=\"width:{100 - TextWidth(context)}%;height:100%;position:relative;display:flex;align-items:center;justify-content:center;padding-top:{reserve}px;\"><div style=\"position:relative;width:{context.Px(340)}px;height:{context.Px(340)}px;background:rgba(255,255,255,0.12);border:{context.Px(4)}px solid rgba(255,255,255,0.28);border-radius:50%;display:flex;align-items:center;justify-content:center;\"><div style=\"color:{context.Palette.Accent};\">{icon}</div></div></div>";
    }

    private static string RenderTextColumn(IconEventRenderContext context, int reserve)
    {
        var body = IconEventPlanFragments.RenderBody(context, "right");
        var paragraphs = body.Length == 0 ? string.Empty : $"<div style=\"margin-bottom:{context.Tokens.ParagraphGap + 12}px;\">{body}</div>";
        var chips = IconEventPlanFragments.RenderMetaChips(context, "flex-start");
        return $"<div class=\"fit-frame\" style=\"width:{TextWidth(context)}%;height:100%;padding:{reserve + 60}px {context.Tokens.Margin + 20}px {context.Tokens.Margin + 40}px {context.Tokens.Margin + 20}px;display:flex;flex-direction:column;justify-content:center;text-align:right;\"><div style=\"width:{context.Px(96)}px;height:{context.Px(8)}px;background:{context.Palette.Accent};margin-bottom:{context.Tokens.ParagraphGap + 4}px;\"></div><h1 style=\"font-size:{context.Px(76)}px;font-weight:900;color:#fff;margin:0 0 {context.Tokens.ParagraphGap + 12}px;line-height:1.25;letter-spacing:-0.5px;text-align:right;\">{context.Headline}</h1>{paragraphs}{chips}</div>";
    }
}
