namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="MediaMonitoringController.RunQuickAiToolAsync"/>.</summary>
/// <param name="Tool">The tool key: <c>generate</c>, <c>tone</c>, <c>rephrase</c>, <c>rewrite</c>, <c>headlines</c>, <c>summary</c>, or <c>messages</c>.</param>
/// <param name="Input">The caller-supplied input text.</param>
/// <param name="Tone">The optional requested tone.</param>
/// <param name="Count">The optional requested count (used by <c>headlines</c>).</param>
public sealed record RunQuickAiToolRequest(string Tool, string Input, string? Tone, int? Count);
