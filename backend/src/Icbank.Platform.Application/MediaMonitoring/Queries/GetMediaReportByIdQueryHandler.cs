using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Queries;

/// <summary>Handles <see cref="GetMediaReportByIdQuery"/>.</summary>
public sealed class GetMediaReportByIdQueryHandler : IRequestHandler<GetMediaReportByIdQuery, Result<MediaReportDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetMediaReportByIdQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetMediaReportByIdQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<MediaReportDto>> Handle(GetMediaReportByIdQuery request, CancellationToken cancellationToken)
    {
        MediaReport? report = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.MediaReports.Where(r => r.Id == request.ReportId), cancellationToken);
        return report is null
            ? Result<MediaReportDto>.Failure("التقرير غير موجود")
            : Result<MediaReportDto>.Success(MediaReportMapper.ToDto(report));
    }
}
