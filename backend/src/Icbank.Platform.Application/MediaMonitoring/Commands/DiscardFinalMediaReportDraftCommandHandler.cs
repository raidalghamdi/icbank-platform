using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="DiscardFinalMediaReportDraftCommand"/>.</summary>
public sealed class DiscardFinalMediaReportDraftCommandHandler : IRequestHandler<DiscardFinalMediaReportDraftCommand, Result<bool>>
{
    private readonly Common.Interfaces.IApplicationDbContext _dbContext;
    private readonly Common.Interfaces.IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="DiscardFinalMediaReportDraftCommandHandler"/> class.</summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="queryExecutor">The async query executor.</param>
    public DiscardFinalMediaReportDraftCommandHandler(
        Common.Interfaces.IApplicationDbContext dbContext, Common.Interfaces.IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DiscardFinalMediaReportDraftCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Domain.MediaMonitoring.FinalMediaReport? report = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.FinalMediaReports.Where(r => r.Id == request.ReportId), cancellationToken);
        if (report is null)
        {
            return Result<bool>.Failure(UpdateFinalMediaReportDraftCommand.NotFoundError);
        }

        if (report.Status != Domain.MediaMonitoring.FinalMediaReportStatus.Draft)
        {
            return Result<bool>.Failure(UpdateFinalMediaReportDraftCommand.LockedError);
        }

        // Why: deletes are soft, and the report number stays unique across deleted rows, so the
        // discarded draft gives its number up; the next generated report then reuses it.
        report.ReportNumber = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{report.ReportNumber}-X{report.Id}");
        _dbContext.Remove(report);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
