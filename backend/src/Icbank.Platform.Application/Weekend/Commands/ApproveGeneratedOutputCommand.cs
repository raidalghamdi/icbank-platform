using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>
/// Ports <c>POST /week-start/approve</c> (API-SURFACE.md §8, BUSINESS-RULES.md §2.5). Marks a
/// generated output as selected and synchronously archives it (the Node source fired this
/// re-archival via a non-awaited <c>setImmediate</c> fire-and-forget, which R-BE-060 forbids for
/// periodic/background work and which this port avoids by awaiting it inline — a deliberate
/// reliability improvement, see WAVE1-PORT-NOTES.md). Re-embedding the archived text is deferred
/// (no embedding provider is wired in this port).
/// </summary>
/// <param name="ActorUserId">The approving user's id.</param>
/// <param name="OutputId">The generated output being approved.</param>
public sealed record ApproveGeneratedOutputCommand(int ActorUserId, int OutputId) : IRequest<Result<GeneratedOutputDto>>;
