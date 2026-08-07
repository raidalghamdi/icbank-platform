using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>Logs out the current session by revoking every active refresh token for the user (API-SURFACE.md §2 <c>POST /auth/logout</c>).</summary>
/// <param name="UserId">The authenticated user's id, or <c>null</c> if the caller wasn't authenticated (logout is a no-op then).</param>
public sealed record LogoutCommand(int? UserId) : IRequest<Result<bool>>;
