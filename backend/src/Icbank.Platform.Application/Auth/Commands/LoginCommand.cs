using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>
/// Email+password login (API-SURFACE.md §2 <c>POST /auth/login</c>). Locks the account after 5
/// consecutive failed attempts (BUSINESS-RULES.md §10.5) and rejects login for an expired
/// password before issuing any token.
/// </summary>
/// <param name="Email">The account email.</param>
/// <param name="Password">The plaintext password, verified against the stored hash and never logged.</param>
/// <param name="IpAddress">The caller's IP address, recorded on the issued refresh token for forensic audit.</param>
public sealed record LoginCommand(string Email, string Password, string? IpAddress) : IRequest<Result<LoginResultDto>>;
