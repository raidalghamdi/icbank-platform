using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Infrastructure.Rendering;

namespace Icbank.Platform.Infrastructure.Shorfah;

/// <summary>
/// Real <see cref="IShorfahDocxRenderer"/> implementation, replacing the Wave 4a placeholder
/// (<c>PlainTextShorfahDocxRenderer</c>, which returned raw UTF-8 text bytes with a spoofed
/// <c>.docx</c> content type). Builds a real OOXML <c>.docx</c> package via
/// <c>DocumentFormat.OpenXml</c> and <see cref="OpenXmlDocxPackageWriter"/>: the title becomes a
/// bold heading paragraph, and the plain-text body (already markdown-stripped by
/// <see cref="ShorfahIssuePlainTextBuilder"/>/<see cref="MarkdownStripper"/>) is split on blank
/// lines into individual right-to-left paragraphs so multi-paragraph structure survives the
/// round trip. Every paragraph gets explicit RTL paragraph/run properties via
/// <see cref="OpenXmlRtlHelpers"/> rather than relying on Word's client-side autodetection.
/// </summary>
public sealed class OpenXmlShorfahDocxRenderer : IShorfahDocxRenderer
{
    /// <inheritdoc />
    public async Task<byte[]> RenderAsync(string titleAr, string plainTextBody, CancellationToken cancellationToken)
    {
        var combinedLength = System.Text.Encoding.UTF8.GetByteCount(titleAr) + System.Text.Encoding.UTF8.GetByteCount(plainTextBody);
        RenderingGuard.EnsureWithinLimit(combinedLength, "Shorfah DOCX text input");

        var docxBytes = await RenderingGuard.RunWithTimeoutAsync(() => Build(titleAr, plainTextBody), cancellationToken);
        RenderingGuard.EnsureWithinLimit(docxBytes.LongLength, "Rendered Shorfah DOCX");
        return docxBytes;
    }

    private static byte[] Build(string titleAr, string plainTextBody)
    {
        var bodyElements = new List<OpenXmlCompositeElement> { OpenXmlRtlHelpers.BuildParagraph(titleAr, bold: true) };
        foreach (var paragraphText in SplitParagraphs(plainTextBody))
        {
            bodyElements.Add(OpenXmlRtlHelpers.BuildParagraph(paragraphText));
        }

        return OpenXmlDocxPackageWriter.Build(bodyElements);
    }

    private static IEnumerable<string> SplitParagraphs(string plainTextBody) =>
        plainTextBody
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line => line.TrimEnd())
            .Where(line => line.Length > 0);
}
