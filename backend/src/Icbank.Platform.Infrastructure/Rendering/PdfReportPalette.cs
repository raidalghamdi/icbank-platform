namespace Icbank.Platform.Infrastructure.Rendering;

/// <summary>
/// The colours the printed report is drawn with. They are held in one place because the cover
/// page, the running chrome and every block fragment have to agree on them: a section badge that
/// drifts a shade away from the cover band is visible the moment the pages sit side by side on a
/// desk. Teal/Navy/Mint/Mustard are the report-template brand colours (BUSINESS-RULES.md §5.7);
/// Green and Magenta carry the strengths-versus-attention contrast the official template relies on
/// to let a reader tell the two halves apart without reading the headers.
/// </summary>
internal static class PdfReportPalette
{
    internal const string Teal = "#1a6e7a";
    internal const string Navy = "#0e3b4a";
    internal const string Band = "#0f4c4a";
    internal const string Mint = "#cce4e6";
    internal const string Mustard = "#b8924a";
    internal const string Green = "#2f7d4f";
    internal const string Magenta = "#b4436c";
    internal const string SectionBand = "#eef2f3";
    internal const string Muted = "#6b7b80";
    internal const string Line = "#dfe6e8";
    internal const string Tint = "#f7f9fa";
    internal const string BandSubtitle = "#cfe0dd";

    /// <summary>Resolves the accent name an indicator card declares to a palette colour.</summary>
    /// <param name="accent">The accent name carried on the card.</param>
    /// <returns>The colour to draw the card's top rule and figure with.</returns>
    internal static string Accent(string accent) => accent switch
    {
        "green" => Green,
        "mustard" => Mustard,
        "magenta" => Magenta,
        _ => Teal,
    };
}
