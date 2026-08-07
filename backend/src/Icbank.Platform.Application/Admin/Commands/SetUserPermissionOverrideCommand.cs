using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>
/// Sets or clears a per-user page/permission override (API-SURFACE.md §5
/// <c>PUT /admin/matrix/user-override</c>). Passing <c>GrantType = null</c> clears any existing
/// override for the (user, page, permission) triple, matching the old system's semantics.
/// </summary>
/// <param name="ActorUserId">The id of the super-admin performing the change (for audit).</param>
/// <param name="TargetUserId">The user the override applies to (SEC-16 resource check — must exist).</param>
/// <param name="PageSlug">The page the override scopes to.</param>
/// <param name="PermissionName">The permission verb the override scopes to.</param>
/// <param name="GrantType">
/// <c>"allow"</c> to grant a permission not held by role, <c>"deny"</c> to revoke one that is,
/// or <c>null</c> to clear any existing override.
/// </param>
public sealed record SetUserPermissionOverrideCommand(
    int ActorUserId, int TargetUserId, string PageSlug, string PermissionName, string? GrantType) : IRequest<Result<bool>>;
