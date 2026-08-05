using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>
/// Replaces a role's page × permission grants (API-SURFACE.md §5 <c>PUT /admin/roles/:id/permissions</c>).
/// Restricted to super-admin callers only (task requirement 4: a plain admin must not be able to
/// change role permissions) and performed transactionally, closing the old system's
/// delete-then-bulk-insert non-transactional pattern (DEFECT-LOG.md DATA-05 pattern).
/// </summary>
/// <param name="ActorUserId">The id of the admin performing the change (for audit).</param>
/// <param name="RoleId">The role whose grants are being replaced.</param>
/// <param name="Grants">The full replacement set of <c>(pageSlug, verb)</c> pairs this role should grant.</param>
public sealed record UpdateRolePermissionsCommand(int ActorUserId, int RoleId, IReadOnlyCollection<(string PageSlug, string PermissionName)> Grants)
    : IRequest<Result<bool>>;
