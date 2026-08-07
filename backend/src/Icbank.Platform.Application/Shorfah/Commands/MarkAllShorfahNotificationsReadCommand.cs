using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>POST /notifications/read-all</c> (API-SURFACE.md §19). Ports <c>shorfah.ts:1023-1029</c>: always scoped to the caller's own id.</summary>
/// <param name="UserId">The authenticated caller's id.</param>
public sealed record MarkAllShorfahNotificationsReadCommand(int UserId) : IRequest<Result<int>>;
