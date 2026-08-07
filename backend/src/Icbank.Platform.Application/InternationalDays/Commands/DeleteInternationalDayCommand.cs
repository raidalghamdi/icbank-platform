using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Commands;

/// <summary>Ports <c>DELETE /intl-days/:id</c> (API-SURFACE.md §14). Hard delete; child themes/activations/sources cascade via real FK constraints.</summary>
/// <param name="ActorUserId">The user performing the delete, for the audit-log write.</param>
/// <param name="DayId">The day id to delete.</param>
public sealed record DeleteInternationalDayCommand(int ActorUserId, int DayId) : IRequest<Result<bool>>;
