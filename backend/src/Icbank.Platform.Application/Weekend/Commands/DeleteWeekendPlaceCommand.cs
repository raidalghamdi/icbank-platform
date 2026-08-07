using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Ports <c>DELETE /weekend-places/:id</c> (API-SURFACE.md §9). Admin-only. Hard delete, matching the Node source (lookup/reference-table style row, per R-BE-023's carve-out).</summary>
/// <param name="ActorUserId">The deleting admin's id, for the audit-log write.</param>
/// <param name="PlaceId">The place being deleted.</param>
public sealed record DeleteWeekendPlaceCommand(int ActorUserId, int PlaceId) : IRequest<Result<bool>>;
