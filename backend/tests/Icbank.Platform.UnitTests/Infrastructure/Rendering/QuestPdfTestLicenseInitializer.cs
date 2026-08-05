using System.Runtime.CompilerServices;

namespace Icbank.Platform.UnitTests.Infrastructure.Rendering;

/// <summary>
/// Sets the QuestPDF Community license once for the whole test assembly, mirroring the real
/// startup registration in <c>Program.cs</c> -- without this, any test that calls
/// <see cref="Icbank.Platform.Infrastructure.Rendering.HtmlDocumentPdfComposer"/> (directly or via
/// a PDF renderer) throws QuestPDF's own license-not-configured exception.
/// </summary>
internal static class QuestPdfTestLicenseInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }
}
