using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Command for <c>POST /cron/shorfah/check-overdue</c> (BUSINESS-RULES.md §1.6). Ports
/// <c>shorfah-cron.ts:21-84</c>: scans every section in <c>pending_contribution</c>/<c>submitted</c>
/// whose <c>SlaDeadline</c> has passed and reminds every assignment. Unlike the Node source (which
/// re-notified on every single invocation with zero dedup, per AMBIGUOUS-BR-2), this port is
/// idempotent: at most one overdue reminder is sent per section/recipient per calendar day
/// (Asia/Riyadh), closing the "168 duplicate emails a week" defect the audit called out.
/// </summary>
public sealed record CheckShorfahOverdueSectionsCommand : IRequest<Result<CheckShorfahOverdueSectionsResultDto>>;
