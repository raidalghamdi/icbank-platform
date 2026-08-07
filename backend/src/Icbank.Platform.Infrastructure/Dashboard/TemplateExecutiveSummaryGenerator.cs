using Icbank.Platform.Application.Dashboard;

namespace Icbank.Platform.Infrastructure.Dashboard;

/// <summary>
/// Deterministic, non-AI default <see cref="IExecutiveSummaryGenerator"/> implementation. The
/// Node source called Gemini (via an Anthropic-shaped adapter) with the verbatim prompt in
/// BUSINESS-RULES.md §9; wiring a real LLM provider is deferred for Wave 1 (see
/// WAVE1-PORT-NOTES.md) — this implementation instead formats the digest into readable Arabic
/// bullet points so the endpoint is fully functional end-to-end without an external AI
/// dependency, and can be swapped for a real provider later without touching Application.
/// </summary>
public sealed class TemplateExecutiveSummaryGenerator : IExecutiveSummaryGenerator
{
    private const string BulletPrefix = "• ";

    /// <inheritdoc />
    public Task<string> GenerateAsync(string dataDigest, CancellationToken cancellationToken)
    {
        var lines = dataDigest.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var bulletedSummary = string.Join(Environment.NewLine, lines.Select(line => BulletPrefix + line));
        return Task.FromResult(bulletedSummary);
    }
}
