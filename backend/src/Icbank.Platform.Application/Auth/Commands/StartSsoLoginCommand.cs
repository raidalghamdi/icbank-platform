using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>Begins the server-side PKCE flow (API-SURFACE.md §3 <c>GET /auth/sso/azure/start</c>).</summary>
/// <param name="RequestedRedirect">The caller-supplied post-login redirect target, validated against the allow-list before being persisted (closes SEC-11).</param>
public sealed record StartSsoLoginCommand(string? RequestedRedirect) : IRequest<Result<string>>;
