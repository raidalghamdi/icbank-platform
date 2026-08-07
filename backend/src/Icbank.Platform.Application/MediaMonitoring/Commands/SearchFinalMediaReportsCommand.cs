using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Searches the final-report archive in one of two modes (<c>POST /final-media-reports/search</c>,
/// BUSINESS-RULES.md §5.5). Closes DEFECT-LOG.md SEC-02: the Node source ran this unauthenticated
/// AI-cost endpoint (info mode) with no authentication at all; this port requires
/// <c>media_monitoring:view</c>. Every query is logged to <see cref="Domain.MediaMonitoring.ReportsQaQuery"/>
/// regardless of mode, matching the Node source exactly.
/// </summary>
/// <param name="ActorUserId">The id of the authenticated caller searching.</param>
/// <param name="Mode">The search mode: <c>full</c> (returns matched report rows, no AI call) or <c>info</c> (AI Q&amp;A over matched excerpts).</param>
/// <param name="Query">The free-text search/question text.</param>
/// <param name="Limit">The maximum number of reports to match, defaults to 5 if omitted.</param>
public sealed record SearchFinalMediaReportsCommand(int ActorUserId, string Mode, string Query, int? Limit)
    : IRequest<Result<SearchFinalMediaReportsResultDto>>;
