using System.Text;
using Icbank.Platform.Application.Shorfah;

namespace Icbank.Platform.Infrastructure.Shorfah;

/// <summary>
/// Deterministic, non-OOXML default <see cref="IShorfahDocxRenderer"/> implementation. The Node
/// source used the <c>docx</c> npm package to build a real <c>.docx</c> OOXML binary; wiring an
/// OOXML-writing library (e.g. <c>DocumentFormat.OpenXml</c>, matching the pattern the office/docx
/// skill uses) is deferred for Wave 4a (see WAVE4A-PORT-NOTES.md), following the exact same
/// deferral pattern as Wave 3a's <c>TemplateFinalReportPdfRenderer</c>. This implementation
/// returns the UTF-8 bytes of the title + plain-text body joined with a blank line, so the
/// docx-export endpoint is fully exercisable end-to-end (persistence, authorization, section
/// selection, audit log) without an external document-writing dependency. The returned bytes are
/// intentionally plain text, not a binary OOXML package; callers must not assume a valid
/// <c>.docx</c> ZIP structure until a real renderer is wired in a follow-up wave.
/// </summary>
public sealed class PlainTextShorfahDocxRenderer : IShorfahDocxRenderer
{
    /// <inheritdoc />
    public Task<byte[]> RenderAsync(string titleAr, string plainTextBody, CancellationToken cancellationToken) =>
        Task.FromResult(Encoding.UTF8.GetBytes($"{titleAr}\n\n{plainTextBody}"));
}
