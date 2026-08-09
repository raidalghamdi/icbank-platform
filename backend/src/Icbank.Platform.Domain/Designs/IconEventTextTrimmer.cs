namespace Icbank.Platform.Domain.Designs;

/// <summary>Normalises copy for rendering without altering its wording.</summary>
public static class IconEventTextTrimmer
{
    /// <summary>Collapses all runs of whitespace into single spaces.</summary>
    /// <param name="text">The copy to normalise.</param>
    /// <returns>The single-spaced copy.</returns>
    public static string Collapse(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
