using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>
/// Ports <c>POST /designs/templates/reseed-presentation</c>, <c>reseed-v2</c>, and
/// <c>reseed-2026</c> (API-SURFACE.md §17, BUSINESS-RULES.md §7.1) as one parametrized command --
/// all 3 Node routes are byte-for-byte the same idempotent-overwrite-by-name algorithm applied to
/// a different hardcoded template array, so this port collapses them into a single handler with
/// a <see cref="Icbank.Platform.Application.Designs.Composer.DesignTemplateSeedSet"/> discriminator
/// rather than duplicating the loop 3 times, mirroring WAVE1-PORT-NOTES.md's precedent for
/// <c>POST /daily-report</c> vs <c>POST /report</c>.
/// </summary>
/// <param name="ActorUserId">The authenticated caller's user id, for audit.</param>
/// <param name="SeedSet">Which named seed set to apply.</param>
public sealed record ReseedDesignTemplateSetCommand(int ActorUserId, DesignTemplateSeedSet SeedSet) : IRequest<Result<ReseedDesignTemplateSetResultDto>>;
