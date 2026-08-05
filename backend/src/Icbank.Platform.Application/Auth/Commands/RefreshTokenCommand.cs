using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>
/// Refreshes an access token from the httpOnly refresh-token cookie (API-SURFACE.md §2
/// <c>POST /auth/refresh</c>). Rotation happens unconditionally — the presented token is revoked
/// and a new one issued on every call, closing the "single-use" requirement.
/// </summary>
/// <param name="RawRefreshToken">The raw refresh-token value read from the cookie.</param>
/// <param name="IpAddress">The caller's IP address, recorded on the newly issued refresh token.</param>
public sealed record RefreshTokenCommand(string RawRefreshToken, string? IpAddress) : IRequest<Result<LoginResultDto>>;
