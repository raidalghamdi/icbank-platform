using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>
/// Ports <c>GET /weekend/published</c> (API-SURFACE.md §10, BUSINESS-RULES.md §2.2). Falls back
/// to the most recently published draft of any date if none matches the target date exactly. The
/// target date defaults to the next Thursday computed in Asia/Riyadh local time (closes the
/// Node source's timezone bug — see <see cref="WeekendCadenceCalculator"/>).
/// </summary>
/// <param name="TargetDate">The requested target date, or <c>null</c> to default to the next Riyadh Thursday.</param>
public sealed record GetPublishedWeekendDraftQuery(string? TargetDate) : IRequest<Result<WeekendDraftDto?>>;
