namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>Options for one <see cref="IGeminiClient"/> call, mirroring the Node source's <c>geminiText</c>/<c>geminiJSON</c> call sites.</summary>
/// <param name="Model">The primary model to try first; defaults are read from <see cref="GeminiOptions"/> by callers.</param>
/// <param name="SystemInstruction">Optional system-role text.</param>
/// <param name="MaxOutputTokens">Defaults to 2048, matching the Node source.</param>
/// <param name="Temperature">Defaults to 0.7, matching the Node source.</param>
/// <param name="UseGoogleSearchTool">Enables the <c>google_search</c> grounding tool.</param>
/// <param name="RequireGrounding">When <c>true</c> (only meaningful together with <see cref="UseGoogleSearchTool"/>), a response with no grounding metadata is treated as a failure rather than a result.</param>
public sealed record GeminiCallOptions(
    string Model,
    string? SystemInstruction = null,
    int MaxOutputTokens = GeminiClient.DefaultMaxOutputTokens,
    double Temperature = GeminiClient.DefaultTemperature,
    bool UseGoogleSearchTool = false,
    bool RequireGrounding = false);
