namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>One turn of content to send to Gemini's <c>generateContent</c> endpoint.</summary>
/// <param name="Model">The model id to call (e.g. <c>gemini-2.5-flash</c>).</param>
/// <param name="SystemInstruction">Optional system-role text, sent as Gemini's <c>systemInstruction</c>.</param>
/// <param name="UserPrompt">The user-role prompt text.</param>
/// <param name="MaxOutputTokens">Maps to <c>generationConfig.maxOutputTokens</c>. Node default: 2048.</param>
/// <param name="Temperature">Maps to <c>generationConfig.temperature</c>. Node default: 0.7.</param>
/// <param name="UseGoogleSearchTool">When <c>true</c>, includes the <c>google_search</c> tool so Gemini may ground its answer in a live web search.</param>
/// <param name="ResponseMimeType">Optional <c>generationConfig.responseMimeType</c> (e.g. <c>application/json</c>), or <c>null</c> for plain text.</param>
public sealed record GeminiGenerationRequest(
    string Model,
    string? SystemInstruction,
    string UserPrompt,
    int MaxOutputTokens,
    double Temperature,
    bool UseGoogleSearchTool,
    string? ResponseMimeType);
