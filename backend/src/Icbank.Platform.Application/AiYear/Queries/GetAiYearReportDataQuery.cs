using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>
/// Ports the data-assembly half of <c>POST /ai-year/report</c> (API-SURFACE.md §13): computes the
/// exact aggregates and "top 3 by reach" ranking (BUSINESS-RULES.md §3: reach-only, no engagement
/// or recency weighting) the Node source fed into its DOCX builder. The actual <c>.docx</c> byte
/// generation (Node source used the <c>docx</c> npm package with Arabic RTL formatting) is
/// deferred -- no DOCX-generation dependency exists in <c>backend/</c> yet (see
/// WAVE2-PORT-NOTES.md, following the Wave 1 storage-deferral pattern). This query still fully
/// exercises and unit-tests the report's actual business logic (aggregation + ranking).
/// </summary>
public sealed record GetAiYearReportDataQuery : IRequest<Result<AiYearReportDataDto>>;
