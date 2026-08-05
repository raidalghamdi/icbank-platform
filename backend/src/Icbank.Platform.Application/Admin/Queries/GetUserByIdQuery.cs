using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>Fetches a single user's admin-facing detail (API-SURFACE.md §5 <c>GET /admin/users/:id</c>).</summary>
/// <param name="ActorUserId">The id of the admin performing the read (for SEC-16 resource-level authorization).</param>
/// <param name="ActorIsSuperAdmin">Whether the actor holds the super-admin capability.</param>
/// <param name="TargetUserId">The user id being looked up.</param>
public sealed record GetUserByIdQuery(int ActorUserId, bool ActorIsSuperAdmin, int TargetUserId) : IRequest<Result<UserDetailDto>>;
