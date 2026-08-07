using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="SearchFinalMediaReportsCommand"/>. Every query is logged to <see cref="ReportsQaQuery"/> regardless of mode, matching the Node source.</summary>
public sealed class SearchFinalMediaReportsCommandHandler : IRequestHandler<SearchFinalMediaReportsCommand, Result<SearchFinalMediaReportsResultDto>>
{
    private const int DefaultLimit = 5;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IReportArchiveQaEngine _qaEngine;

    /// <summary>Initializes a new instance of the <see cref="SearchFinalMediaReportsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="qaEngine">The archive Q&amp;A port.</param>
    public SearchFinalMediaReportsCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IReportArchiveQaEngine qaEngine)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _qaEngine = qaEngine;
    }

    /// <inheritdoc />
    public async Task<Result<SearchFinalMediaReportsResultDto>> Handle(SearchFinalMediaReportsCommand request, CancellationToken cancellationToken)
    {
        var limit = request.Limit ?? DefaultLimit;
        var pattern = request.Query.Trim();
        List<FinalMediaReport> matches = await _queryExecutor.ToListAsync(
            _dbContext.FinalMediaReports
                .Where(r => r.Title.Contains(pattern) || r.PeriodLabel.Contains(pattern) || (r.ExecutiveSummary != null && r.ExecutiveSummary.Contains(pattern)))
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit),
            cancellationToken);

        SearchFinalMediaReportsResultDto result = request.Mode == "full"
            ? new SearchFinalMediaReportsResultDto("full", matches.Select(FinalMediaReportMapper.ToSummaryDto).ToList(), null)
            : await BuildInfoModeResultAsync(request, matches, cancellationToken);

        LogQuery(request, matches);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<SearchFinalMediaReportsResultDto>.Success(result);
    }

    private async Task<SearchFinalMediaReportsResultDto> BuildInfoModeResultAsync(
        SearchFinalMediaReportsCommand request, List<FinalMediaReport> matches, CancellationToken cancellationToken)
    {
        var context = ReportArchiveContextBuilder.Build(matches);
        var answer = await _qaEngine.AnswerAsync(request.Query, context, cancellationToken);
        return new SearchFinalMediaReportsResultDto("info", null, answer);
    }

    private void LogQuery(SearchFinalMediaReportsCommand request, List<FinalMediaReport> matches)
    {
        QaQueryType queryType = request.Mode == "full" ? QaQueryType.SearchFull : QaQueryType.SearchInfo;
        _dbContext.Add(new ReportsQaQuery
        {
            UserId = request.ActorUserId,
            QueryType = queryType,
            SearchQuery = request.Query,
            ResultSummary = $"{matches.Count} تقرير مطابق",
        });
    }
}
