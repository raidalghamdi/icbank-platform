using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>
/// Ports the Node source's template-aware spatial-hint injection for AI background generation
/// verbatim (BUSINESS-RULES.md §7.3, <c>designs.ts:407-428</c>). Magic thresholds (0.55, 0.3, 0.4
/// of canvas height) are exactly as documented -- not configurable, not decided by this port.
/// </summary>
public static class BackgroundPromptBuilder
{
    private const double BottomThirdThreshold = 0.55;
    private const double TopThirdYThreshold = 0.3;
    private const double TopThirdHeightThreshold = 0.4;
    private const string BottomHint = "Leave the bottom third of the image visually calm, low-contrast, and uncluttered — a semi-transparent text-overlay panel will cover that region.";
    private const string TopHint = "Leave the top third of the image visually calm and uncluttered — a semi-transparent text-overlay panel will cover that region.";
    private const string GenericHint = "Leave a calm, low-contrast visual zone in the lower portion for a text overlay panel.";
    private const string QualitySuffix = "Professional high-quality photo, 16:9 widescreen aspect ratio, no text or watermarks.";

    /// <summary>Builds the full prompt, appending the spatial hint (if a template with a background panel is supplied) and the fixed quality suffix.</summary>
    /// <param name="basePrompt">The caller-supplied prompt text.</param>
    /// <param name="template">The optional template to derive the spatial hint from.</param>
    /// <returns>The fully-assembled prompt.</returns>
    public static string Build(string basePrompt, DesignTemplate? template)
    {
        var hint = template?.BackgroundPanelConfig is { } panel ? ResolveSpatialHint(panel, template!.CanvasHeight) : null;
        IEnumerable<string?> parts = new[] { basePrompt.Trim(), hint, QualitySuffix }.Where(p => !string.IsNullOrEmpty(p));
        return string.Join(' ', parts);
    }

    private static string ResolveSpatialHint(BackgroundPanelConfig panel, int canvasHeight)
    {
        var height = canvasHeight <= 0 ? 1080 : canvasHeight;
        if (panel.Y > height * BottomThirdThreshold)
        {
            return BottomHint;
        }

        if (panel.Y < height * TopThirdYThreshold && panel.Height < height * TopThirdHeightThreshold)
        {
            return TopHint;
        }

        return GenericHint;
    }
}
