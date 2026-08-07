using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Auth.Queries;

/// <summary>Current-user profile + effective permissions (API-SURFACE.md §2 <c>GET /auth/me</c>).</summary>
/// <param name="UserId">The authenticated user's id, resolved from the access token's subject claim.</param>
public sealed record GetCurrentUserQuery(int UserId) : IRequest<Result<AuthenticatedUserDto>>;
