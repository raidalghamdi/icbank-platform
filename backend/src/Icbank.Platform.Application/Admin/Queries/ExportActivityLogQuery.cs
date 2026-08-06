using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>
/// Exports the activity/audit log as CSV (API-SURFACE.md §5, old Node <c>GET
/// /admin/activity/export</c> at <c>admin.ts:637</c> — ported here because the .NET port missed
/// it entirely, leaving the frontend's export button 404ing). Unlike <see cref="ListActivityLogQuery"/>
/// this is a purpose-built, row-capped export endpoint per DOTNET-CONVENTIONS.md §8's
/// R-BE-033-vs-exports interpretation: no page/pageSize, just the same filters plus a hard row
/// ceiling (see <see cref="ExportActivityLogQueryHandler.MaxRows"/>) mirroring the Node
/// original's <c>.limit(5000)</c>. Exporting the full log is itself a security-relevant action,
/// so the handler writes an audit-log entry for every successful export (who exported, with
/// which filters, how many rows).
/// </summary>
/// <param name="ActorUserId">The authenticated caller's id, recorded on the audit entry.</param>
/// <param name="UserId">Optional filter to a single acting user (exact match, mirrors the list endpoint).</param>
/// <param name="Action">Optional filter to an exact action name (exact match, mirrors the list endpoint — the Node original used a substring ILIKE, but this port follows the already-established exact-match convention of <see cref="ListActivityLogQuery"/> rather than reintroducing a second, inconsistent filter semantic).</param>
/// <param name="DateFrom">Optional inclusive lower bound (UTC).</param>
/// <param name="DateTo">Optional inclusive upper bound (UTC).</param>
public sealed record ExportActivityLogQuery(
    int ActorUserId,
    int? UserId,
    string? Action,
    DateTime? DateFrom,
    DateTime? DateTo) : IRequest<Result<ActivityLogExportDto>>;
