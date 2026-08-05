using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Queries;

/// <summary>Ports <c>GET /designs/templates/:id</c> (API-SURFACE.md §17).</summary>
/// <param name="TemplateId">The template id.</param>
public sealed record GetDesignTemplateByIdQuery(int TemplateId) : IRequest<Result<DesignTemplateDto>>;
