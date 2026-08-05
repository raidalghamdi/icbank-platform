using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Ports <c>GET /weekend/drafts/:id</c> (API-SURFACE.md §10). Admin-only.</summary>
/// <param name="DraftId">The draft being fetched.</param>
public sealed record GetWeekendDraftByIdQuery(int DraftId) : IRequest<Result<WeekendDraftDto>>;
