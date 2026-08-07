using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Forces a password reset (API-SURFACE.md §5 <c>POST /admin/users/:id/reset-password</c>).</summary>
/// <param name="ActorUserId">The id of the admin performing the reset (for audit).</param>
/// <param name="ActorIsSuperAdmin">Whether the actor holds the super-admin capability (SEC-16 resource check).</param>
/// <param name="TargetUserId">The user whose password is being reset.</param>
public sealed record ResetUserPasswordCommand(int ActorUserId, bool ActorIsSuperAdmin, int TargetUserId) : IRequest<Result<string>>;
