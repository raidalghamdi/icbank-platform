using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Ports <c>DELETE /designs/templates/:id</c> (API-SURFACE.md §17).</summary>
/// <param name="ActorUserId">The authenticated caller's user id, for audit.</param>
/// <param name="TemplateId">The template id to delete.</param>
public sealed record DeleteDesignTemplateCommand(int ActorUserId, int TemplateId) : IRequest<Result<bool>>;
