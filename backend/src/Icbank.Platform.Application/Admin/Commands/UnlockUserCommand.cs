using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Clears a user's lockout and resets their failed-attempt counter (API-SURFACE.md §5 <c>POST /admin/users/:id/unlock</c>).</summary>
/// <param name="ActorUserId">The id of the admin performing the unlock (for audit).</param>
/// <param name="TargetUserId">The locked user's id.</param>
public sealed record UnlockUserCommand(int ActorUserId, int TargetUserId) : IRequest<Result<bool>>;
