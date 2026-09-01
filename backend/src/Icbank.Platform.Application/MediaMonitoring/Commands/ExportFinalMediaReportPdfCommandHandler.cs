using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="ExportFinalMediaReportPdfCommand"/>.</summary>
public sealed class ExportFinalMediaReportPdfCommandHandler : IRequestHandler<ExportFinalMediaReportPdfCommand, Result<byte[]>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IFinalReportPdfRenderer _pdfRenderer;

    /// <summary>Initializes a new instance of the <see cref="ExportFinalMediaReportPdfCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="pdfRenderer">The PDF rendering port.</param>
    public ExportFinalMediaReportPdfCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IFinalReportPdfRenderer pdfRenderer)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _pdfRenderer = pdfRenderer;
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> Handle(ExportFinalMediaReportPdfCommand request, CancellationToken cancellationToken)
    {
        FinalMediaReport? report = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.FinalMediaReports.Where(r => r.Id == request.ReportId), cancellationToken);
        if (report is null)
        {
            return Result<byte[]>.Failure("التقرير غير موجود");
        }

        FinalMediaReportDetailDto detail = FinalMediaReportMapper.ToDetailDto(report);
        var html = FinalReportHtmlBuilder.Build(detail);
        var footerLabel = detail.Summary.ReportNumber + " · " + detail.Summary.PeriodLabel;
        var pdfBytes = await _pdfRenderer.RenderAsync(html, footerLabel, cancellationToken);
        return Result<byte[]>.Success(pdfBytes);
    }
}
