using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Generates the canonical 8-section final-report draft from cached feed data
/// (<c>POST /final-media-reports/generate</c>, BUSINESS-RULES.md §5.3). Closes DEFECT-LOG.md
/// SEC-02: the Node source ran this unauthenticated AI-cost endpoint with no authentication at
/// all; this port requires <c>media_monitoring:create</c>. This command only produces a draft --
/// it never persists a <c>FinalMediaReport</c> row itself (matching the Node source's
/// generate/save split); persistence happens via a separate explicit
/// <see cref="CreateFinalMediaReportCommand"/> call.
/// </summary>
/// <param name="ActorUserId">The id of the authenticated caller generating the draft.</param>
/// <param name="PeriodLabel">The human-readable period label.</param>
/// <param name="Audience">The free-text target audience description.</param>
/// <param name="DateFrom">The range start.</param>
/// <param name="DateTo">The range end.</param>
/// <param name="FocusTopics">Optional focus-topics free text.</param>
public sealed record GenerateFinalMediaReportCommand(
    int ActorUserId, string PeriodLabel, string? Audience, DateTimeOffset DateFrom, DateTimeOffset DateTo, string? FocusTopics)
    : IRequest<Result<GenerateFinalMediaReportResultDto>>;
