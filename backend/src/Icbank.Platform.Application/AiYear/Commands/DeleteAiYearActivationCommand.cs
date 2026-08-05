using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Commands;

/// <summary>
/// Ports <c>DELETE /ai-year/activations/:id</c> (API-SURFACE.md §13). The Node source's redundant
/// inline role check (already covered by <c>requirePageAccess("ai_year")</c>) is not re-added
/// here -- this port relies solely on the controller's <c>[Authorize(Policy = "ai_year:delete")]</c>
/// attribute, avoiding the second maintenance point the Node source's duplication created.
/// </summary>
/// <param name="ActorUserId">The user performing the delete, for the audit-log write.</param>
/// <param name="ActivationId">The activation id to delete.</param>
public sealed record DeleteAiYearActivationCommand(int ActorUserId, int ActivationId) : IRequest<Result<bool>>;
