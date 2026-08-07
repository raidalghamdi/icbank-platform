using QuestPDF.Infrastructure;

namespace Icbank.Platform.Infrastructure.Rendering;

/// <summary>
/// Registers the GAC-approved Frutiger LT Arabic typeface (bundled as embedded resources in this
/// assembly, mirroring <c>artifacts/internal-comms/fonts/frutiger/</c>) with QuestPDF's font
/// manager so PDF rendering never depends on a system font being present in the container, and so
/// generated documents carry the same face as the web UI. QuestPDF resolves fonts by family name
/// at render time; this type must run its one-time registration before any
/// <see cref="QuestPDF.Fluent.Document"/> is composed.
/// </summary>
/// <remarks>
/// The shipped TTFs have normalised <c>name</c> records. The vendor files disagree with each other
/// — the 55 Roman calls itself "Frutiger LT Arabic 55 Roman" and the 65 Bold reports a family of
/// "Frutiger LT Arabic 45 Light" with a Bold subfamily. Registered as-is they land as three
/// unrelated families and QuestPDF's weight selection silently falls back to Regular for bold
/// text. Names were rewritten onto one family so Regular and Bold resolve as one pair.
///
/// LICENSING: these binaries carry Monotype/Linotype name records with fsType=4 (Preview and
/// Print). Confirm GAC's licence covers embedding in generated documents before production use,
/// and replace them with the files from GAC's official brand package. See
/// artifacts/internal-comms/fonts/frutiger/PROVENANCE.md.
/// </remarks>
public static class EmbeddedArabicFontProvider
{
    /// <summary>The font family name QuestPDF should use for every Arabic/RTL text style.</summary>
    public const string FontFamily = "Frutiger LT Arabic";

    private const string ResourcePrefix = "Icbank.Platform.Infrastructure.Rendering.Fonts.";

    /// <summary>
    /// Regular and Bold share one family so QuestPDF can pair them for weighted text. Light
    /// carries its own family name because a four-style family cannot hold a third upright
    /// weight; it is registered so callers that ask for it by name resolve rather than fall back.
    /// </summary>
    private static readonly string[] FontResources =
    {
        "FrutigerLTArabic-Roman.ttf",
        "FrutigerLTArabic-Bold.ttf",
        "FrutigerLTArabic-Light.ttf",
    };

    private static readonly object RegistrationLock = new();
    private static bool _registered;

    /// <summary>
    /// Registers every embedded weight with QuestPDF's global font manager. Idempotent and
    /// thread-safe -- safe to call from every renderer constructor.
    /// </summary>
    public static void EnsureRegistered()
    {
        if (_registered)
        {
            return;
        }

        lock (RegistrationLock)
        {
            if (_registered)
            {
                return;
            }

            foreach (var fileName in FontResources)
            {
                using Stream fontStream = OpenFontStream(fileName);
                QuestPDF.Drawing.FontManager.RegisterFont(fontStream);
            }

            _registered = true;
        }
    }

    private static Stream OpenFontStream(string fileName)
    {
        var resourceName = ResourcePrefix + fileName;
        System.Reflection.Assembly assembly = typeof(EmbeddedArabicFontProvider).Assembly;
        Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded font resource '{resourceName}' was not found. Verify the .csproj EmbeddedResource entry and the resource's assembly-qualified name.");
        }

        return stream;
    }
}
