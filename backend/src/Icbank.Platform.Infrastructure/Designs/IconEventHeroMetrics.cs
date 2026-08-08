namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Derived type and spacing values for the content-aware hero composition.</summary>
internal sealed record IconEventHeroMetrics(
    bool IsDense,
    bool IsVeryDense,
    double SizeScale,
    int MainIconSize,
    int SubtitleSize,
    int SubtitleMaxWidth,
    int TitleSize,
    int TitleGap,
    int HeaderReserve,
    int FooterReserve,
    int IconBoxSize,
    int IconTextGap,
    int ParagraphGap);
