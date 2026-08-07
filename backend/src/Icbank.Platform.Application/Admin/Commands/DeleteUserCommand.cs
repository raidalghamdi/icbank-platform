using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>
/// Soft-deletes a user account (API-SURFACE.md §5 <c>DELETE /admin/users/:id</c>). The old system
/// hard-deleted the row with no cascade audit; this port sets <c>DeletedAt</c> instead
/// (R-BE-023 — deletes are soft, never <c>DbSet.Remove</c> on a business table), which is both
/// the mandated convention and a deliberate behaviour change flagged for product sign-off in
/// AUTH-PORT-NOTES.md (a deleted user's history remains queryable/auditable).
/// </summary>
/// <param name="ActorUserId">The id of the admin performing the deletion (for audit; also enforces "cannot delete self").</param>
/// <param name="ActorIsSuperAdmin">Whether the actor holds the super-admin capability (SEC-16 resource check).</param>
/// <param name="TargetUserId">The user being deleted.</param>
public sealed record DeleteUserCommand(int ActorUserId, bool ActorIsSuperAdmin, int TargetUserId) : IRequest<Result<bool>>;
