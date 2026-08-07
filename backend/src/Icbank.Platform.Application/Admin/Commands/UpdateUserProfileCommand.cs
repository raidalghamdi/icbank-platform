using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>
/// Updates a user's profile fields (API-SURFACE.md §5 <c>PATCH /admin/users/:id</c>). Deliberately
/// does not accept a <c>roleId</c> field — the old system's version of this endpoint let any
/// admin change a user's role (including granting <c>super_admin</c>) in the same call
/// (DEFECT-LOG.md SEC-01). Role changes are a distinct, super-admin-gated operation
/// (<c>POST /admin/users/:id/roles</c>, <see cref="AssignUserRoleCommand"/>) in this port — a
/// deliberate behaviour change called out in AUTH-PORT-NOTES.md.
/// </summary>
/// <param name="ActorUserId">The id of the admin performing the update (for audit).</param>
/// <param name="ActorIsSuperAdmin">Whether the actor holds the super-admin capability (SEC-16 resource check).</param>
/// <param name="TargetUserId">The user being updated.</param>
/// <param name="Name">The new display name, if changing.</param>
/// <param name="Title">The new job title, if changing.</param>
/// <param name="Department">The new department, if changing.</param>
/// <param name="Email">The new email address, if changing.</param>
public sealed record UpdateUserProfileCommand(
    int ActorUserId,
    bool ActorIsSuperAdmin,
    int TargetUserId,
    string? Name,
    string? Title,
    string? Department,
    string? Email) : IRequest<Result<UserDetailDto>>;
