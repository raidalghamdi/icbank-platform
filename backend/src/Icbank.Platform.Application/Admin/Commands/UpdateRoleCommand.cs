using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Edits a role's display fields (API-SURFACE.md §5 <c>PATCH /admin/roles/:id</c>). The machine <c>Name</c> is immutable once created — only display metadata changes.</summary>
/// <param name="ActorUserId">The id of the super-admin performing the edit (for audit).</param>
/// <param name="RoleId">The role being edited (SEC-16 resource check — must exist).</param>
/// <param name="NameAr">The new Arabic display label, if changing.</param>
/// <param name="Description">The new description, if changing.</param>
public sealed record UpdateRoleCommand(int ActorUserId, int RoleId, string? NameAr, string? Description) : IRequest<Result<bool>>;
