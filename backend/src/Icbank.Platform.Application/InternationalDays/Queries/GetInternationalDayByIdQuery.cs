using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Queries;

/// <summary>Ports <c>GET /intl-days/:id</c> (API-SURFACE.md §14).</summary>
/// <param name="DayId">The day id to fetch.</param>
public sealed record GetInternationalDayByIdQuery(int DayId) : IRequest<Result<InternationalDayDetailDto>>;
