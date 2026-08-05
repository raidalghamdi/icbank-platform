using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Ports <c>POST /designs/templates/seed-test</c> (API-SURFACE.md §17): no-op if any template already exists.</summary>
/// <param name="ActorUserId">The authenticated caller's user id, for audit.</param>
public sealed record SeedTestDesignTemplateCommand(int ActorUserId) : IRequest<Result<SeedTestDesignTemplateResultDto>>;
