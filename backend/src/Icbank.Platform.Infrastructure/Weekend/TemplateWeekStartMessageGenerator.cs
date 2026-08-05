using Icbank.Platform.Application.Weekend;

namespace Icbank.Platform.Infrastructure.Weekend;

/// <summary>
/// Deterministic, non-AI default <see cref="IWeekStartMessageGenerator"/> implementation. The
/// Node source called Claude/GPT-4o/Gemini in parallel (all three actually Gemini-backed) with
/// the verbatim prompt in BUSINESS-RULES.md §2.5; wiring a real LLM provider chain is deferred
/// for Wave 1 (see WAVE1-PORT-NOTES.md) — this implementation produces one clearly-labeled
/// placeholder message per model so the archive/approve pipeline is fully exercisable end-to-end.
/// </summary>
public sealed class TemplateWeekStartMessageGenerator : IWeekStartMessageGenerator
{
    private static readonly string[] ModelNames = { "claude", "openai", "gemini" };

    /// <inheritdoc />
    public Task<IReadOnlyList<WeekStartModelOutput>> GenerateAsync(WeekStartGenerationRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<WeekStartModelOutput> outputs = ModelNames
            .Select(modelName => new WeekStartModelOutput(modelName, $"رسالة بداية أسبوع مؤقتة عن: {request.Topic} ({modelName})"))
            .ToList();

        return Task.FromResult(outputs);
    }
}
