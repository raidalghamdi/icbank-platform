using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>
/// Assigns a role to a user (API-SURFACE.md §5 <c>PATCH /admin/users/:id</c> role-change facet,
/// now split into its own explicit action). This is the single write path that can grant
/// <c>super_admin</c> — enforcement that only a super-admin caller may use it at all, and that a
/// caller may never grant a role more privileged than they themselves hold, happens in the
/// controller via the distinct <c>super-admin</c> authorization policy (closes SEC-01). The
/// handler additionally asserts the same rule server-side so it can never be bypassed by a future
/// caller that forgets the attribute.
/// </summary>
/// <param name="ActorUserId">The id of the admin performing the assignment (for audit + the SEC-01 self-escalation check).</param>
/// <param name="ActorIsSuperAdmin">Whether the acting admin holds the super-admin capability.</param>
/// <param name="TargetUserId">The user being assigned a role.</param>
/// <param name="RoleId">The role to assign.</param>
public sealed record AssignUserRoleCommand(int ActorUserId, bool ActorIsSuperAdmin, int TargetUserId, int RoleId) : IRequest<Result<bool>>;
