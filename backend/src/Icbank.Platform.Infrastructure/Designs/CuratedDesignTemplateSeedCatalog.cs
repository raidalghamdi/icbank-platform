using Icbank.Platform.Application.Designs.Composer;
using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>
/// Default <see cref="IDesignTemplateSeedCatalog"/> implementation. Ships the real template
/// names from the Node source's 3 seed files with a structurally faithful but simplified layout
/// (single background panel + one title/body text slot + one logo slot) rather than every exact
/// pixel coordinate, gradient/badge/icon-grid extra, and per-template font-size tuning the
/// originals define -- porting ~650 lines of hand-tuned layout constants pixel-for-pixel was
/// judged lower value than the idempotent seed/upsert business logic itself for this wave (see
/// WAVE3B-PORT-NOTES.md). Swappable for a pixel-exact catalog without touching Application.
/// </summary>
public sealed class CuratedDesignTemplateSeedCatalog : IDesignTemplateSeedCatalog
{
    private const int PresentationWidth = 1920;
    private const int PresentationHeight = 1440;
    private const int SquareSize = 1080;
    private const int Year2026Width = 1920;
    private const int Year2026Height = 1080;

    /// <inheritdoc />
    public IReadOnlyList<DesignTemplateSeedDefinition> GetSeedSet(DesignTemplateSeedSet seedSet) =>
        seedSet switch
        {
            DesignTemplateSeedSet.Presentation => BuildPresentationSet(),
            DesignTemplateSeedSet.SocialV2 => BuildSocialV2Set(),
            DesignTemplateSeedSet.Year2026 => BuildYear2026Set(),
            _ => Array.Empty<DesignTemplateSeedDefinition>(),
        };

    private static List<DesignTemplateSeedDefinition> BuildPresentationSet() => new()
    {
        BuildDefinition("شريحة عرض — فقرات", "presentation", PresentationWidth, PresentationHeight),
        BuildDefinition("شريحة عرض — أيقونات 4", "presentation", PresentationWidth, PresentationHeight),
    };

    private static List<DesignTemplateSeedDefinition> BuildSocialV2Set() => new()
    {
        BuildDefinition("منشور مربع — عرض / خصم", "social", SquareSize, SquareSize),
        BuildDefinition("منشور مربع — إعلان ورشة", "social", SquareSize, SquareSize),
        BuildDefinition("منشور مربع — تهنئة", "social", SquareSize, SquareSize),
        BuildDefinition("غلاف فيسبوك — رسمي", "social", 1640, 924),
        BuildDefinition("غلاف تويتر — رسمي", "social", 1500, 500),
        BuildDefinition("صورة تويتر — رسمية", "social", 1600, 900),
    };

    private static List<DesignTemplateSeedDefinition> BuildYear2026Set() => new()
    {
        BuildDefinition("إعلان مؤسسي 2026 — رسمي", "announcement", Year2026Width, Year2026Height),
        BuildDefinition("إعلان ورشة 2026 — Instagram 4:5", "social", 1080, 1350),
        BuildDefinition("منشور سوشيال 2026 — حديث", "social", SquareSize, SquareSize),
    };

    private static DesignTemplateSeedDefinition BuildDefinition(string nameAr, string category, int width, int height)
    {
        var background = new BackgroundPanelConfig { X = 0, Y = height * 0.65, Width = width, Height = height * 0.35, Color = "#0e3b4a", Opacity = 0.88 };
        var textSlots = new List<TextSlot>
        {
            new() { Key = "title", LabelAr = "العنوان الرئيسي", Role = "title", X = width * 0.05, Y = height * 0.7, Width = width * 0.9, Height = height * 0.12, DefaultFontSize = width * 0.035, MaxWords = 10, Alignment = "right", Color = "#ffffff" },
            new() { Key = "body", LabelAr = "النص التفصيلي", Role = "body", X = width * 0.05, Y = height * 0.83, Width = width * 0.9, Height = height * 0.12, DefaultFontSize = width * 0.02, MaxWords = 30, Alignment = "right", Color = "#d0dcff" },
        };
        var logoSlots = new List<LogoSlot> { new() { Key = "logo_main", X = width * 0.04, Y = height * 0.03, MaxWidth = width * 0.2, MaxHeight = height * 0.12, Align = "right" } };

        return new DesignTemplateSeedDefinition(nameAr, category, width, height, background, textSlots, logoSlots, PromptHint: null, Extras: null);
    }
}
