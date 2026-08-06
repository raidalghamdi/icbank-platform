using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Commands;

/// <summary>
/// Mints a short-lived, single-use download token scoped to one international day's HTML export
/// (GAP 2 -- FRONTEND-WIRING-NOTES.md §4: <c>idExport()</c> used <c>window.open(...)</c>, a plain
/// browser navigation that carries no bearer header under the new JWT-only auth).
/// </summary>
/// <param name="ActorUserId">The authenticated caller's id, recorded on the minted token for forensic audit.</param>
/// <param name="DayId">The international day id the token is scoped to.</param>
public sealed record IssueInternationalDayDownloadTokenCommand(int ActorUserId, int DayId) : IRequest<Result<string>>;
