using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Command for <c>POST /notifications/{id}/read</c> (API-SURFACE.md §19). Ports
/// <c>shorfah.ts:1011-1021</c>: the update is scoped to <c>UserId</c> in addition to the
/// notification id, so a caller can never mark another user's notification read (the primary
/// IDOR surface named by the task brief; the controller additionally runs a SEC-16
/// existence-and-ownership check before this command is even dispatched).
/// </summary>
/// <param name="UserId">The authenticated caller's id.</param>
/// <param name="NotificationId">The notification being marked read.</param>
public sealed record MarkShorfahNotificationReadCommand(int UserId, int NotificationId) : IRequest<Result<bool>>;
