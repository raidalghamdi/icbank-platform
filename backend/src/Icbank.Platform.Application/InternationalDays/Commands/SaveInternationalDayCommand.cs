using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Commands;

/// <summary>
/// Ports <c>POST /intl-days/save</c> (API-SURFACE.md §14): persists a search result across the
/// day, its yearly theme, activations, design samples (stored as activations with
/// <c>activation_type = "تصميم بصري"</c>, matching the Node source exactly), and sources.
/// </summary>
/// <param name="ActorUserId">The user performing the save, for the audit-log write.</param>
/// <param name="Data">The search result to persist. Validated by <see cref="DaySearchResultValidator"/> before any write (closes DEFECT-LOG.md DATA-04/H-2).</param>
/// <param name="Category">The optional category to tag the day with.</param>
public sealed record SaveInternationalDayCommand(int ActorUserId, DaySearchResultDto Data, string? Category) : IRequest<Result<SaveInternationalDayResultDto>>;
