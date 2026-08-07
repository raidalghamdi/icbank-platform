using System.Text;
using Icbank.Platform.Application.Designs.Composer;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>
/// Deterministic, non-Gemini default <see cref="IBackgroundImageGenerator"/> implementation.
/// Returns the UTF-8 bytes of the prompt it was given (labeled as a placeholder), matching the
/// same never-fabricate-real-bytes contract as <c>TemplateFinalReportPdfRenderer</c>.
/// </summary>
public sealed class TemplateBackgroundImageGenerator : IBackgroundImageGenerator
{
    private const string PlaceholderContentType = "text/plain";

    /// <inheritdoc />
    public Task<GeneratedBackgroundImage> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        var placeholder = Encoding.UTF8.GetBytes($"GENERATED-BACKGROUND-PLACEHOLDER prompt={prompt}");
        return Task.FromResult(new GeneratedBackgroundImage(placeholder, PlaceholderContentType));
    }
}
