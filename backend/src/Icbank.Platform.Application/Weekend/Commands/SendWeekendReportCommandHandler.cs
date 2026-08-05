using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>
/// Handles <see cref="SendWeekendReportCommand"/>. Closes DEFECT-LOG.md BUG-01: no channel is
/// ever reported as dispatched/queued, because no email/SMS/WhatsApp provider is wired in this
/// port — every channel with a non-empty target honestly reports <c>not_implemented</c>. This is
/// a deliberate behaviour change from the Node source (which always fabricated
/// <c>status:'queued', ok:true</c>) — see WAVE1-PORT-NOTES.md.
/// </summary>
public sealed class SendWeekendReportCommandHandler : IRequestHandler<SendWeekendReportCommand, Result<SendWeekendReportResultDto>>
{
    private const string NotImplementedStatus = "not_implemented";
    private const string EmptyTargetError = "فارغ";
    private const string UnsupportedChannelError = "نوع قناة غير مدعوم";
    private static readonly HashSet<string> SupportedChannelTypes = new(StringComparer.OrdinalIgnoreCase) { "email", "sms", "whatsapp" };

    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="SendWeekendReportCommandHandler"/> class.</summary>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public SendWeekendReportCommandHandler(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<SendWeekendReportResultDto>> Handle(SendWeekendReportCommand request, CancellationToken cancellationToken)
    {
        var results = request.Channels.Select(EvaluateChannel).ToList();

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "weekend_report.send_attempt",
            "WeekendReport",
            request.Period,
            before: null,
            after: new { request.Provider, ChannelCount = request.Channels.Count },
            cancellationToken);

        var dto = new SendWeekendReportResultDto(Ok: false, request.Period, request.Provider, request.Channels.Count, Dispatched: 0, results);
        return Result<SendWeekendReportResultDto>.Success(dto);
    }

    private static WeekendReportChannelResultDto EvaluateChannel(WeekendReportChannel channel)
    {
        var type = channel.Type.Trim().ToLowerInvariant();
        var target = channel.To.Trim();

        if (string.IsNullOrEmpty(target))
        {
            return new WeekendReportChannelResultDto(type, target, Ok: false, Status: "rejected", Error: EmptyTargetError);
        }

        return SupportedChannelTypes.Contains(type)
            ? new WeekendReportChannelResultDto(type, target, Ok: false, NotImplementedStatus, Error: null)
            : new WeekendReportChannelResultDto(type, target, Ok: false, Status: "rejected", Error: UnsupportedChannelError);
    }
}
