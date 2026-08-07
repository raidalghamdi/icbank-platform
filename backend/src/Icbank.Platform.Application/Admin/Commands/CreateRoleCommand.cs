using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Creates a custom (non-system) role (API-SURFACE.md §5 <c>POST /admin/roles</c>).</summary>
/// <param name="ActorUserId">The id of the super-admin performing the creation (for audit).</param>
/// <param name="Name">The role's unique machine name.</param>
/// <param name="NameAr">The role's Arabic display label.</param>
/// <param name="Description">An optional description.</param>
public sealed record CreateRoleCommand(int ActorUserId, string Name, string NameAr, string? Description) : IRequest<Result<RoleSummaryDto>>;
