using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Ports <c>POST /shorfah/issues/:id/send-initial</c> (API-SURFACE.md §19, BUSINESS-RULES.md
/// §1.6, §1.7). Admin-only. For every section in the issue, starts its SLA clock (<c>SlaStartsAt
/// = now</c>, <c>SlaDeadline = now + SlaDays</c>) and sends an "initial contribution request"
/// notification to every assignment on that section. Rate-limited via the same limiter family as
/// the other AI/email-cost endpoints (task requirement: this route is a cost-abuse vector since
/// it fans out real email sends).
/// </summary>
/// <param name="ActorUserId">The admin's id.</param>
/// <param name="IssueId">The issue whose sections' SLA clocks are being started.</param>
public sealed record SendShorfahIssueInitialCommand(int ActorUserId, int IssueId) : IRequest<Result<SendShorfahIssueInitialResultDto>>;
