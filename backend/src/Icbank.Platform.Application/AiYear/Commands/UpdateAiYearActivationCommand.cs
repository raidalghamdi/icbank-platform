using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Commands;

/// <summary>
/// Ports <c>PUT /ai-year/activations/:id</c> (API-SURFACE.md §13). All fields are optional
/// (partial update semantics matching the Node source); when <see cref="Media"/> or
/// <see cref="Metrics"/> is supplied (non-null), the existing rows for that child collection are
/// replaced wholesale, matching the Node source's delete-then-insert behavior. Closes
/// DEFECT-LOG.md DATA-05: the whole operation is wrapped in one <c>SaveChangesAsync</c> call
/// (one implicit transaction) instead of the Node source's un-transactioned 3-table sequence.
/// </summary>
/// <param name="ActorUserId">The user performing the update, for the audit-log write.</param>
/// <param name="ActivationId">The activation id.</param>
/// <param name="Title">The new title, if changing.</param>
/// <param name="Month">The new month, if changing.</param>
/// <param name="ActivationDate">The new free-text activation date, if changing.</param>
/// <param name="Type">The new activation type, if changing.</param>
/// <param name="Description">The new description, if changing.</param>
/// <param name="Tags">The new tag list, if changing.</param>
/// <param name="Status">The new status, if changing.</param>
/// <param name="Reach">The new reach metric, if changing.</param>
/// <param name="Engagement">The new engagement metric, if changing.</param>
/// <param name="Notes">The new notes, if changing.</param>
/// <param name="Channels">The full replacement channel list, if changing.</param>
/// <param name="Media">The full replacement media list, validated against <see cref="AiYearMediaPathValidator"/>, if changing.</param>
/// <param name="Metrics">The full replacement metrics list, if changing.</param>
public sealed record UpdateAiYearActivationCommand(
    int ActorUserId,
    int ActivationId,
    string? Title,
    int? Month,
    string? ActivationDate,
    string? Type,
    string? Description,
    IReadOnlyList<string>? Tags,
    string? Status,
    int? Reach,
    int? Engagement,
    string? Notes,
    IReadOnlyList<string>? Channels,
    IReadOnlyList<CreateAiYearActivationMediaItem>? Media,
    IReadOnlyList<CreateAiYearActivationMetricItem>? Metrics) : IRequest<Result<AiYearActivationDto>>;
