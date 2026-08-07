using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Creates a new user account (API-SURFACE.md §5 <c>POST /admin/users</c>).</summary>
/// <param name="ActorUserId">The id of the admin performing the creation (for audit).</param>
/// <param name="ActorIsSuperAdmin">Whether the actor holds the super-admin capability (SEC-01: only a super-admin may create an account pre-assigned the super_admin role).</param>
/// <param name="Email">The new user's email address.</param>
/// <param name="Name">The new user's display name.</param>
/// <param name="Title">The new user's job title, if any.</param>
/// <param name="Department">The new user's department, if any.</param>
/// <param name="RoleId">The role to assign at creation time.</param>
/// <param name="Password">An optional caller-supplied initial password; a random one is generated and returned once if omitted.</param>
public sealed record CreateUserCommand(
    int ActorUserId,
    bool ActorIsSuperAdmin,
    string Email,
    string Name,
    string? Title,
    string? Department,
    int RoleId,
    string? Password) : IRequest<Result<CreateUserResult>>;
