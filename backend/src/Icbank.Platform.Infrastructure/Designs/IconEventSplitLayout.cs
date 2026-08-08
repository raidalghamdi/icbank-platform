using System.Globalization;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Renders the landscape-family icon-and-copy split composition.</summary>
internal static class IconEventSplitLayout
{
    internal static string Render(IconEventRenderContext context)
    {
        IconEventParagraphFlow flow = IconEventParagraphFlowBuilder.Build(context.Input.Subtitle, context.Input.ContactEmail, context.Input.ContactPhone);
        var reserve = context.Tokens.Margin + (context.Tokens.DeptPaddingV * 2) + context.Tokens.DeptFont + 60;
        return $"<div class=\"poster split-layout\" style=\"{PosterStyle(context)}display:flex;\">{Chrome(context)}{RenderIconColumn(context, reserve)}{RenderTextColumn(context, flow, reserve)}{Footer(context)}</div>";
    }

    private static string Chrome(IconEventRenderContext context) =>
        IconEventHtmlFragments.RenderDepartment(context.Input.Department, context) + IconEventHtmlFragments.RenderLogo(context.Input.LogoUrl, $"position:absolute;top:{context.Tokens.Margin}px;right:{context.Tokens.Margin}px;z-index:10", context.Tokens.LogoHeight, context.Input.Size);

    private static string Footer(IconEventRenderContext context) =>
        $"<div style=\"position:absolute;bottom:0;left:0;right:0;height:{context.Px(12)}px;background:linear-gradient(90deg,{context.Palette.Accent} 0%,{context.Palette.Secondary} 50%,{context.Palette.Primary} 100%);\"></div>";

    private static string PosterStyle(IconEventRenderContext context) =>
        $"width:{context.Width}px;height:{context.Height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('{IconEventVisualAssets.StatsHeroBackgroundDataUri}');background-size:cover;background-position:center;";

    private static string RenderIconColumn(IconEventRenderContext context, int reserve)
    {
        var icon = IconEventIconLibrary.Render(context.Input.MainIcon, context.Px(240), context.Palette.Accent);
        return $"<div style=\"width:40%;height:100%;position:relative;display:flex;align-items:center;justify-content:center;padding-top:{reserve}px;\"><div style=\"position:relative;width:{context.Px(340)}px;height:{context.Px(340)}px;background:rgba(255,255,255,0.12);border:{context.Px(4)}px solid rgba(255,255,255,0.28);border-radius:50%;display:flex;align-items:center;justify-content:center;\"><div style=\"color:{context.Palette.Accent};\">{icon}</div></div></div>";
    }

    private static string RenderTextColumn(IconEventRenderContext context, IconEventParagraphFlow flow, int reserve)
    {
        var renderedFlow = IconEventHtmlFragments.RenderParagraphFlow(flow, ParagraphStyle(context), context.Palette, context.Tokens.MetaFont, "right");
        var paragraphs = IconEventHtmlFragments.HasTextBlocks(flow) ? $"<div style=\"display:flex;flex-direction:column;gap:{context.Tokens.ParagraphGap - 4}px;margin-bottom:{context.Tokens.ParagraphGap + 12}px;text-align:right;\">{renderedFlow}</div>" : string.Empty;
        var chips = RenderChips(context, flow);
        return $"<div style=\"width:60%;height:100%;padding:{reserve + 60}px {context.Tokens.Margin + 20}px {context.Tokens.Margin + 40}px {context.Tokens.Margin + 20}px;display:flex;flex-direction:column;justify-content:center;text-align:right;\"><div style=\"width:{context.Px(96)}px;height:{context.Px(8)}px;background:{context.Palette.Accent};margin-bottom:{context.Tokens.ParagraphGap + 4}px;\"></div><h1 style=\"font-size:{context.Px(76)}px;font-weight:900;color:#fff;margin:0 0 {context.Tokens.ParagraphGap + 12}px;line-height:1.25;letter-spacing:-0.5px;text-align:right;\">{context.Headline}</h1>{paragraphs}{(chips.Length > 0 ? $"<div style=\"display:flex;gap:{context.Px(14)}px;flex-wrap:wrap;\">{chips}</div>" : string.Empty)}</div>";
    }

    private static string RenderChips(IconEventRenderContext context, IconEventParagraphFlow flow)
    {
        return Meta(context, "calendar", context.Input.Date) + Meta(context, "clock", context.Input.Time) + Meta(context, "map-pin", context.Input.Location) + Contacts(context, flow);
    }

    private static string Contacts(IconEventRenderContext context, IconEventParagraphFlow flow)
    {
        var email = !flow.EmailUsedInline && !string.IsNullOrWhiteSpace(context.Input.ContactEmail) ? IconEventHtmlFragments.RenderContactChip("mail", context.Input.ContactEmail, context.Palette, context.Tokens.MetaFont) : string.Empty;
        var phone = !flow.PhoneUsedInline && !string.IsNullOrWhiteSpace(context.Input.ContactPhone) ? IconEventHtmlFragments.RenderContactChip("phone", context.Input.ContactPhone, context.Palette, context.Tokens.MetaFont) : string.Empty;
        return email + phone;
    }

    private static string Meta(IconEventRenderContext context, string icon, string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : IconEventHtmlFragments.RenderMetaChip(icon, value, context.Palette, context.Tokens.MetaFont);

    private static string ParagraphStyle(IconEventRenderContext context) =>
        $"font-size:{context.Tokens.SubtitleSize}px;color:#fff;margin:0;line-height:{(context.Tokens.LineHeight - 0.1).ToString(CultureInfo.InvariantCulture)};font-weight:500;opacity:0.95;";
}
