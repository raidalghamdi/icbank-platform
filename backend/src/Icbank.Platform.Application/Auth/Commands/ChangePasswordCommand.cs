using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>Changes the authenticated caller's local password and clears the mandatory-reset flag.</summary>
/// <param name="UserId">The authenticated caller.</param>
/// <param name="CurrentPassword">The current password, verified before any mutation.</param>
/// <param name="NewPassword">The replacement password, never logged or returned.</param>
public sealed record ChangePasswordCommand(int UserId, string CurrentPassword, string NewPassword) : IRequest<Result<bool>>;
