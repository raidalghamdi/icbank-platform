using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>
/// Toggles a user's active/suspended state (API-SURFACE.md §5 <c>POST /admin/users/:id/suspend</c>,
/// which the source system implements as a toggle — this port keeps the same semantics rather
/// than splitting into separate suspend/reactivate endpoints, since the task explicitly lists
/// "suspend/reactivate" as one pairing).
/// </summary>
/// <param name="ActorUserId">The id of the admin performing the toggle (for audit; also enforces "cannot suspend self").</param>
/// <param name="ActorIsSuperAdmin">Whether the actor holds the super-admin capability (SEC-16 resource check).</param>
/// <param name="TargetUserId">The user whose active state is being toggled.</param>
public sealed record SetUserSuspensionCommand(int ActorUserId, bool ActorIsSuperAdmin, int TargetUserId) : IRequest<Result<bool>>;
