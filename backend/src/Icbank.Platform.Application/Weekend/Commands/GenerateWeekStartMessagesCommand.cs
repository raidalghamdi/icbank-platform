using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Ports <c>POST /week-start/generate</c> (API-SURFACE.md §8, BUSINESS-RULES.md §2.5). Returns all 3 model outputs synchronously (see <see cref="Weekend.IWeekStartMessageGenerator"/> for the SSE-vs-synchronous deviation note).</summary>
/// <param name="ActorUserId">The requesting user's id.</param>
/// <param name="Topic">The message topic.</param>
/// <param name="Occasion">The occasion, if any.</param>
/// <param name="Audience">The target audience, if any.</param>
/// <param name="Tone">The desired tone.</param>
/// <param name="Length">The desired length option.</param>
public sealed record GenerateWeekStartMessagesCommand(int ActorUserId, string Topic, string? Occasion, string? Audience, string? Tone, string? Length)
    : IRequest<Result<IReadOnlyList<GeneratedOutputDto>>>;
