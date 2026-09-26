using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="UpdateFinalMediaReportDraftCommand"/>.</summary>
public sealed class UpdateFinalMediaReportDraftCommandHandler : IRequestHandler<UpdateFinalMediaReportDraftCommand, Result<FinalMediaReportDetailDto>>
{
    private const int TitleMaxLength = 300;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="UpdateFinalMediaReportDraftCommandHandler"/> class.</summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="queryExecutor">The async query executor.</param>
    public UpdateFinalMediaReportDraftCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<FinalMediaReportDetailDto>> Handle(UpdateFinalMediaReportDraftCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var error = Validate(request);
        if (error is not null)
        {
            return Result<FinalMediaReportDetailDto>.Failure(error);
        }

        FinalMediaReport? report = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.FinalMediaReports.Where(r => r.Id == request.ReportId), cancellationToken);
        if (report is null)
        {
            return Result<FinalMediaReportDetailDto>.Failure(UpdateFinalMediaReportDraftCommand.NotFoundError);
        }

        if (report.Status != FinalMediaReportStatus.Draft)
        {
            return Result<FinalMediaReportDetailDto>.Failure(UpdateFinalMediaReportDraftCommand.LockedError);
        }

        report.Title = request.Title.Trim();
        FinalReportDraftApplier.Apply(report, request.Draft);
        report.LayoutJson = FinalReportLayout.Parse(request.LayoutJson).ToJson();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<FinalMediaReportDetailDto>.Success(FinalMediaReportMapper.ToDetailDto(report));
    }

    private static string? Validate(UpdateFinalMediaReportDraftCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > TitleMaxLength)
        {
            return UpdateFinalMediaReportDraftCommand.EmptyTitleError;
        }

        return request.Draft is null ? "محتوى التقرير مطلوب" : FinalReportLayout.Validate(request.LayoutJson);
    }
}
