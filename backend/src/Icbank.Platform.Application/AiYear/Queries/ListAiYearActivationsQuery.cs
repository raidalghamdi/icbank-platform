using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>Ports <c>GET /ai-year/activations</c> (API-SURFACE.md §13). Closes DEFECT-LOG.md DATA-06's N+1 pattern via batched media/metric lookups.</summary>
/// <param name="Query">The pagination request.</param>
/// <param name="Month">Optional month filter.</param>
/// <param name="Type">Optional type filter.</param>
/// <param name="Channel">Optional channel filter.</param>
/// <param name="SearchText">Optional fuzzy search on title/description.</param>
public sealed record ListAiYearActivationsQuery(PagedQuery Query, int? Month, string? Type, string? Channel, string? SearchText)
    : IRequest<Result<PagedResult<AiYearActivationDto>>>;
