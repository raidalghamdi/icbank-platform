using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>
/// Completes the Azure AD authorization-code flow entirely server-side (API-SURFACE.md §3
/// <c>GET /auth/sso/azure/callback</c>). The result carries only a raw refresh token and access
/// token for the Api layer to set as an httpOnly cookie / return in JSON — never embedded in
/// server-rendered HTML/JS (closes SEC-04/SEC-05).
/// </summary>
/// <param name="Code">The authorization code returned by Azure AD.</param>
/// <param name="State">The opaque state value, matched against the server-side PKCE store.</param>
/// <param name="IpAddress">The caller's IP address, recorded on the issued refresh token.</param>
public sealed record SsoCallbackCommand(string Code, string State, string? IpAddress) : IRequest<Result<SsoCallbackResultDto>>;
