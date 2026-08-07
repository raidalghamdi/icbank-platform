using Icbank.Platform.Application.MediaMonitoring;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Deterministic, non-AI default <see cref="IPromptExecutionEngine"/> implementation. The Node
/// source called Gemini directly for both <c>POST /prompts/:id/run</c> and <c>POST /ai/quick</c>;
/// wiring a real LLM provider is deferred for Wave 3a (see WAVE3A-PORT-NOTES.md) -- this
/// implementation echoes a clearly-labeled placeholder so every downstream endpoint is fully
/// exercisable end-to-end without an external AI dependency.
/// </summary>
public sealed class TemplatePromptExecutionEngine : IPromptExecutionEngine
{
    private const int PromptPreviewMaxLength = 200;

    /// <inheritdoc />
    public Task<string> ExecuteAsync(string promptText, CancellationToken cancellationToken)
    {
        var preview = promptText.Length > PromptPreviewMaxLength ? promptText[..PromptPreviewMaxLength] : promptText;
        var output = $"[نموذج مؤقت بانتظار ربط مزوّد الذكاء الاصطناعي]\n\nالمُدخل: {preview}";
        return Task.FromResult(output);
    }
}
