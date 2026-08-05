using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Runs one of the 7 fixed "AI Quick" text tools (<c>POST /ai/quick</c>, BUSINESS-RULES.md §5.6).
/// Closes DEFECT-LOG.md SEC-02: the Node source ran this unauthenticated AI-cost endpoint with no
/// authentication at all; this port requires <c>media_monitoring:view</c>.
/// </summary>
/// <param name="ActorUserId">The id of the authenticated caller running the tool.</param>
/// <param name="Tool">The tool key: <c>generate</c>, <c>tone</c>, <c>rephrase</c>, <c>rewrite</c>, <c>headlines</c>, <c>summary</c>, or <c>messages</c>.</param>
/// <param name="Input">The caller-supplied input text.</param>
/// <param name="Tone">The optional requested tone.</param>
/// <param name="Count">The optional requested count (used by <c>headlines</c>).</param>
public sealed record RunQuickAiToolCommand(int ActorUserId, string Tool, string Input, string? Tone, int? Count)
    : IRequest<Result<RunQuickAiToolResultDto>>;
