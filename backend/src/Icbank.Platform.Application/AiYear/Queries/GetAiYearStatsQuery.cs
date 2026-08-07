using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>Ports <c>GET /ai-year/stats</c> (API-SURFACE.md §13).</summary>
public sealed record GetAiYearStatsQuery : IRequest<Result<AiYearStatsDto>>;
