using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>
/// Fetches the full effective permission matrix, every user × every page, including per-user
/// overrides (API-SURFACE.md §5 <c>GET /admin/matrix</c>).
/// </summary>
/// <param name="Query">The paging parameters over the user rows (R-BE-033 — the old system returned every user unbounded; this port paginates).</param>
public sealed record GetEffectivePermissionMatrixQuery(PagedQuery Query) : IRequest<Result<EffectivePermissionMatrixDto>>;
