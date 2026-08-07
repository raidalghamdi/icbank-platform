using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Ports <c>POST /designs/generate-backgrounds</c> (API-SURFACE.md §17, BUSINESS-RULES.md §7.3).</summary>
/// <param name="ActorUserId">The authenticated caller's user id, for audit and rate limiting.</param>
/// <param name="Prompt">The base image prompt.</param>
/// <param name="TemplateId">The optional template id, used to derive the spatial-awareness hint.</param>
public sealed record GenerateBackgroundsCommand(int ActorUserId, string Prompt, int? TemplateId) : IRequest<Result<GenerateBackgroundsResultDto>>;
