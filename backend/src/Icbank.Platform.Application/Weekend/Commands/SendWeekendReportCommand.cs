using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>
/// Ports <c>POST /weekend/send</c> (API-SURFACE.md §10). Admin-only. The Node source is a stub
/// with no provider wired ("none wired") that always returns <c>status:'queued'</c> for every
/// channel regardless of whether anything was actually dispatched -- [DEFECT-LOG.md BUG-01],
/// fabricated success. This port closes BUG-01: it honestly reports every channel as
/// <c>not_implemented</c> rather than claiming a fake queued/success status. See
/// WAVE1-PORT-NOTES.md -- wiring a real email/SMS/WhatsApp provider is deferred.
/// </summary>
/// <param name="ActorUserId">The requesting admin's id.</param>
/// <param name="Channels">The requested delivery channels (type/target pairs).</param>
/// <param name="Provider">The requested SMS/WhatsApp provider name.</param>
/// <param name="Period">The reporting period label.</param>
public sealed record SendWeekendReportCommand(
    int ActorUserId, IReadOnlyList<WeekendReportChannel> Channels, string Provider, string Period)
    : IRequest<Result<SendWeekendReportResultDto>>;
