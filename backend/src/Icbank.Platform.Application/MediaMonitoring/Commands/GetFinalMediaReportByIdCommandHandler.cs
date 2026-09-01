using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring.Appearance;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="GetFinalMediaReportByIdCommand"/>.</summary>
public sealed class GetFinalMediaReportByIdCommandHandler : IRequestHandler<GetFinalMediaReportByIdCommand, Result<FinalMediaReportDetailDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetFinalMediaReportByIdCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetFinalMediaReportByIdCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<FinalMediaReportDetailDto>> Handle(GetFinalMediaReportByIdCommand request, CancellationToken cancellationToken)
    {
        FinalMediaReport? report = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.FinalMediaReports.Where(r => r.Id == request.ReportId), cancellationToken);
        if (report is null)
        {
            return Result<FinalMediaReportDetailDto>.Failure("التقرير غير موجود");
        }

        report.ViewCount += 1;
        await _dbContext.SaveChangesAsync(cancellationToken);

        MediaAppearanceAnalysisDto appearance = await MediaAppearanceLoader.LoadAsync(
            _dbContext, _queryExecutor, report.DateFrom, report.DateTo, cancellationToken);

        return Result<FinalMediaReportDetailDto>.Success(FinalMediaReportMapper.ToDetailDto(report, appearance));
    }
}
