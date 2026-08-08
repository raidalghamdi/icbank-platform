using System.Text.RegularExpressions;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Calculates hero spacing from the selected canvas and body density.</summary>
internal static class IconEventHeroMetricsFactory
{
    private const int ReferenceHeight = 864;
    private const int ReferenceWidth = 1440;

    private static readonly Regex BulletRegex = new(@"^\s*[*\-•]\s+", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex SubHeadingRegex = new(@"[^\n]{3,80}؟\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    internal static IconEventHeroMetrics Create(IconEventRenderContext context, bool hasBottomChips)
    {
        var subtitle = context.Input.Subtitle ?? string.Empty;
        var bulletCount = BulletRegex.Count(subtitle);
        var subHeadCount = SubHeadingRegex.Count(subtitle);
        var dense = subtitle.Length > 350 || bulletCount >= 3;
        var veryDense = subtitle.Length > 650 || bulletCount >= 5 || subHeadCount >= 2;
        var scale = Math.Sqrt((context.Width * (double)context.Height) / (ReferenceWidth * (double)ReferenceHeight));
        return CreateValues(context, hasBottomChips, dense, veryDense, scale);
    }

    private static IconEventHeroMetrics CreateValues(IconEventRenderContext context, bool hasBottomChips, bool dense, bool veryDense, double scale)
    {
        var mainIcon = Scale(veryDense ? 130 : dense ? 150 : 190, scale, 1);
        var bodySize = Scale(veryDense ? 22 : dense ? 24 : 28, scale, 11);
        var titleSize = Scale(veryDense ? 42 : dense ? 50 : 58, scale, 16);
        var header = context.Tokens.Margin + context.Tokens.LogoHeight + 24;
        var footer = hasBottomChips ? Scale(90, scale, 1) : Scale(40, scale, 1);
        return new IconEventHeroMetrics(dense, veryDense, scale, mainIcon, bodySize, Math.Min((int)(context.Width * 0.85), 1600), titleSize, Scale(veryDense ? 8 : 14, scale, 1), header, footer, mainIcon + Scale(30, scale, 1), Scale(veryDense ? 18 : dense ? 26 : 36, scale, 1), Scale(veryDense ? 10 : 14, scale, 1));
    }

    private static int Scale(int value, double scale, int minimum) => Math.Max(minimum, (int)Math.Round(value * scale));
}
