namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Port for executing a fully-substituted prompt against an AI provider
/// (<c>POST /prompts/:id/run</c>, BUSINESS-RULES.md §5). The Node source called Gemini directly;
/// this port keeps the model call out of Application (R-BE-002) and swappable in Infrastructure.
/// Wave 3a ships a deterministic, non-AI default implementation -- wiring a real LLM call is
/// deferred, see WAVE3A-PORT-NOTES.md.
/// </summary>
public interface IPromptExecutionEngine
{
    /// <summary>Executes the given, already variable-substituted prompt text.</summary>
    /// <param name="promptText">The fully-substituted prompt text sent to the model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The model's generated output text.</returns>
    Task<string> ExecuteAsync(string promptText, CancellationToken cancellationToken);
}
