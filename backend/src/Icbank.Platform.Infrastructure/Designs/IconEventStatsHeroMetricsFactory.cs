namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Maps all maintained output sizes to the landscape reference geometry.</summary>
internal static class IconEventStatsHeroMetricsFactory
{
    private const double ReferenceWidth = 2000d;

    internal static IconEventStatsHeroMetrics Create(IconEventRenderContext context)
    {
        var scale = context.Width / ReferenceWidth;
        return new IconEventStatsHeroMetrics(
            Scale(58, scale),
            Scale(70, scale),
            Scale(266, scale),
            Scale(90, scale),
            Scale(80, scale),
            Scale(18, scale),
            Scale(34, scale),
            Scale(18, scale),
            Scale(235, scale),
            Scale(87, scale),
            Scale(1680, scale),
            Scale(370, scale),
            Scale(44, scale),
            Scale(1640, scale),
            Scale(530, scale),
            Scale(1480, scale),
            Scale(28, scale),
            Scale(104, scale),
            Scale(24, scale),
            Scale(200, scale),
            Scale(30, scale),
            Scale(146, scale),
            Scale(28, scale),
            Scale(26, scale),
            Scale(8, scale),
            Scale(380, scale),
            Scale(105, scale),
            Scale(110, scale),
            Scale(38, scale));
    }

    private static int Scale(int value, double scale) => Math.Max(1, (int)Math.Round(value * scale));
}
