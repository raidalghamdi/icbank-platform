namespace Icbank.Platform.Domain.Designs;

/// <summary>The size presets accepted by the icon-event designer (ports the legacy 3-preset subset of <c>composer/icon-event-composer.ts</c>'s <c>SizePreset</c> union; BUSINESS-RULES.md §7.5).</summary>
public enum IconEventSizePreset
{
    /// <summary>1200×1200 (1:1) square format.</summary>
    Square,

    /// <summary>1200×2133 (9:16) vertical story format.</summary>
    Story,

    /// <summary>2000×1125 (16:9) horizontal landscape format.</summary>
    Landscape,
}
