using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>
/// Soft-deletes a custom role (API-SURFACE.md §5 <c>DELETE /admin/roles/:id</c>). Blocked when
/// <c>IsSystem</c> is set (the 9 seeded roles can never be deleted, matching the old system's
/// <c>is_system=true</c> guard) and when any user currently holds the role.
/// </summary>
/// <param name="ActorUserId">The id of the super-admin performing the deletion (for audit).</param>
/// <param name="RoleId">The role being deleted (SEC-16 resource check — must exist).</param>
public sealed record DeleteRoleCommand(int ActorUserId, int RoleId) : IRequest<Result<bool>>;
