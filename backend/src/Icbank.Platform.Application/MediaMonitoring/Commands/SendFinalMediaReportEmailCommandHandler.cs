using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="SendFinalMediaReportEmailCommand"/>.</summary>
public sealed class SendFinalMediaReportEmailCommandHandler : IRequestHandler<SendFinalMediaReportEmailCommand, Result<SendFinalMediaReportEmailResultDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IReportEmailSender _emailSender;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="SendFinalMediaReportEmailCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="emailSender">The email-sending port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public SendFinalMediaReportEmailCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IReportEmailSender emailSender, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _emailSender = emailSender;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<SendFinalMediaReportEmailResultDto>> Handle(SendFinalMediaReportEmailCommand request, CancellationToken cancellationToken)
    {
        FinalMediaReport? report = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.FinalMediaReports.Where(r => r.Id == request.ReportId), cancellationToken);
        if (report is null)
        {
            return Result<SendFinalMediaReportEmailResultDto>.Failure("التقرير غير موجود");
        }

        var html = FinalReportHtmlBuilder.Build(FinalMediaReportMapper.ToDetailDto(report));
        var subject = request.Subject ?? report.Title;
        ReportEmailResult emailResult = await _emailSender.SendAsync(request.Recipients, subject, html, cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "final_media_report.send_email",
            "FinalMediaReport",
            request.ReportId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { Recipients = request.Recipients, emailResult.Sent },
            cancellationToken);

        return Result<SendFinalMediaReportEmailResultDto>.Success(
            new SendFinalMediaReportEmailResultDto(emailResult.Sent, request.Recipients, emailResult.ProviderMessage));
    }
}
