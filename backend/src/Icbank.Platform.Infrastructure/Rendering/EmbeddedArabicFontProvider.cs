using QuestPDF.Infrastructure;

namespace Icbank.Platform.Infrastructure.Rendering;

/// <summary>
/// Registers the Cairo Arabic-coverage font (bundled as an embedded resource in this assembly,
/// copied from <c>artifacts/internal-comms/public/fonts/cairo/Cairo-Variable.ttf</c>, SIL Open
/// Font License 1.1 -- confirmed via the font's own embedded `name` table records 0/13/14) with
/// QuestPDF's font manager so PDF rendering never depends on a system font being present in the
/// container. QuestPDF resolves fonts by family name at render time; this type must run its
/// one-time registration before any <see cref="QuestPDF.Fluent.Document"/> is composed.
/// </summary>
public static class EmbeddedArabicFontProvider
{
    /// <summary>The font family name QuestPDF should use for every Arabic/RTL text style.</summary>
    public const string FontFamily = "Cairo";

    private const string ResourceName = "Icbank.Platform.Infrastructure.Rendering.Fonts.Cairo-Variable.ttf";
    private static readonly object RegistrationLock = new();
    private static bool _registered;

    /// <summary>
    /// Registers the embedded Cairo font with QuestPDF's global font manager. Idempotent and
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

            using Stream fontStream = OpenFontStream();
            QuestPDF.Drawing.FontManager.RegisterFont(fontStream);
            _registered = true;
        }
    }

    private static Stream OpenFontStream()
    {
        System.Reflection.Assembly assembly = typeof(EmbeddedArabicFontProvider).Assembly;
        Stream? stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded Arabic font resource '{ResourceName}' was not found. Verify the .csproj EmbeddedResource entry and the resource's assembly-qualified name.");
        }

        return stream;
    }
}
