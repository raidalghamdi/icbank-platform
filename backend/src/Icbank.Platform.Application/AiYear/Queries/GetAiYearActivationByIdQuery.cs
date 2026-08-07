using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>Ports <c>GET /ai-year/activations/:id</c> (API-SURFACE.md §13).</summary>
/// <param name="ActivationId">The activation id.</param>
public sealed record GetAiYearActivationByIdQuery(int ActivationId) : IRequest<Result<AiYearActivationDto>>;
