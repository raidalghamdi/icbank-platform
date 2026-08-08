using Icbank.Platform.Application.Designs.IconEvent;
using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Creates complete, self-contained, HTML-encoded icon-event poster documents.</summary>
public sealed class IconEventHtmlComposer : IIconEventHtmlRenderer
{
    /// <inheritdoc />
    public string Render(IconEventInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var context = new IconEventRenderContext(input);
        var inner = RenderLayout(context);
        return $"<!DOCTYPE html><html lang=\"ar\" dir=\"rtl\"><head><meta charset=\"UTF-8\" /><title>{context.Headline}</title><link rel=\"preconnect\" href=\"https://fonts.googleapis.com\"><link rel=\"preconnect\" href=\"https://fonts.gstatic.com\" crossorigin><link href=\"https://fonts.googleapis.com/css2?family=Tajawal:wght@400;500;700;900&family=Cairo:wght@400;700;900&display=swap\" rel=\"stylesheet\"><style>{IconEventVisualAssets.FontCss}*{{box-sizing:border-box;}}body{{margin:0;padding:0;background:#f0f0f0;display:flex;align-items:center;justify-content:center;min-height:100vh;}}.poster{{box-shadow:0 20px 60px rgba(0,0,0,0.2);}}svg{{display:block;}}.hero-content{{overflow:hidden;}}</style></head><body>{inner}{IconEventVisualAssets.AutoFitScript}</body></html>";
    }

    private static string RenderLayout(IconEventRenderContext context) => context.Input.Layout switch
    {
        IconEventLayoutType.StatsHero => IconEventStatsHeroLayout.Render(context),
        IconEventLayoutType.Hero => IconEventHeroLayout.Render(context),
        IconEventLayoutType.Grid => IconEventGridLayout.Render(context),
        IconEventLayoutType.Split => IconEventSplitLayout.Render(context),
        IconEventLayoutType.Typography => IconEventTypographyLayout.Render(context),
        _ => IconEventHeroLayout.Render(context),
    };
}
