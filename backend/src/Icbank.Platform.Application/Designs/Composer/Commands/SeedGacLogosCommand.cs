using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Ports <c>POST /designs/logos/seed-gac</c> (API-SURFACE.md §17): idempotent on <c>logoName</c>.</summary>
/// <param name="ActorUserId">The authenticated caller's user id, for audit.</param>
public sealed record SeedGacLogosCommand(int ActorUserId) : IRequest<Result<SeedGacLogosResultDto>>;
