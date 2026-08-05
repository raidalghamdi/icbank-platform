using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Commands;

/// <summary>Ports <c>POST /ai-year/activations</c> (API-SURFACE.md §13). Wrapped in a single <c>SaveChangesAsync</c> call (implicit transaction).</summary>
/// <param name="ActorUserId">The user performing the create, for the audit-log write.</param>
/// <param name="Title">The activation title.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="Year">The calendar year, defaults to 2026 if omitted.</param>
/// <param name="ActivationDate">The free-text activation date, if known.</param>
/// <param name="Type">The activation type (free text, DATA-MODEL.md AMBIGUOUS-4).</param>
/// <param name="Channels">The distribution channels (at least one required).</param>
/// <param name="Description">A free-text description, if any.</param>
/// <param name="Tags">The tag list.</param>
/// <param name="Status">The lifecycle status, defaults to "Published" if omitted.</param>
/// <param name="Reach">The reach metric, if any.</param>
/// <param name="Engagement">The engagement metric, if any.</param>
/// <param name="Notes">Free-text notes, if any.</param>
/// <param name="Media">The media to attach, validated against <see cref="AiYearMediaPathValidator"/> before any write.</param>
/// <param name="Metrics">The free-form metrics to attach.</param>
public sealed record CreateAiYearActivationCommand(
    int ActorUserId,
    string Title,
    int Month,
    int? Year,
    string? ActivationDate,
    string Type,
    IReadOnlyList<string> Channels,
    string? Description,
    IReadOnlyList<string>? Tags,
    string? Status,
    int? Reach,
    int? Engagement,
    string? Notes,
    IReadOnlyList<CreateAiYearActivationMediaItem>? Media,
    IReadOnlyList<CreateAiYearActivationMetricItem>? Metrics) : IRequest<Result<AiYearActivationDto>>;
