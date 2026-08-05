using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Regenerates just the executive summary of a final report (<c>POST
/// /final-media-reports/:id/exec-summary</c>). Closes DEFECT-LOG.md SEC-02: the Node source ran
/// this unauthenticated AI-cost endpoint with no authentication at all; this port requires
/// <c>media_monitoring:edit</c> -- note the final report row itself is never mutated by this
/// endpoint in the Node source (it returns the regenerated text without persisting it), a
/// behaviour this port preserves exactly.
/// </summary>
/// <param name="ActorUserId">The id of the authenticated caller regenerating the summary.</param>
/// <param name="ReportId">The report id.</param>
public sealed record RegenerateExecutiveSummaryCommand(int ActorUserId, int ReportId) : IRequest<Result<RegenerateExecutiveSummaryResultDto>>;
