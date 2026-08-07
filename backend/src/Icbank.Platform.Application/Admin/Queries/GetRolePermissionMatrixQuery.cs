using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>Fetches a single role's page × permission grant matrix (API-SURFACE.md §5 <c>GET /admin/roles/:id/permissions</c>).</summary>
/// <param name="RoleId">The role being read (SEC-16 resource check — must exist).</param>
public sealed record GetRolePermissionMatrixQuery(int RoleId) : IRequest<Result<RolePermissionMatrixDto>>;
