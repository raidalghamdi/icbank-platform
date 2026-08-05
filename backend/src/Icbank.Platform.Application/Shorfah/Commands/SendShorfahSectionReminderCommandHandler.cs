using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Handles <see cref="SendShorfahSectionReminderCommand"/>. Ports <c>shorfah.ts:957-994</c>: sends
/// a single manual reminder to one recipient. Admin-only, audited, and rate-limited via the
/// existing send-initial limiter (shared cost-abuse-vector budget for outbound Shorfah email).
/// </summary>
public sealed class SendShorfahSectionReminderCommandHandler : IRequestHandler<SendShorfahSectionReminderCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IShorfahNotificationSender _notificationSender;
    private readonly IShorfahUrlProvider _urlProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="SendShorfahSectionReminderCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="notificationSender">The notification dispatch port.</param>
    /// <param name="urlProvider">The configured base-URL port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public SendShorfahSectionReminderCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IDateTimeProvider dateTimeProvider,
        IShorfahNotificationSender notificationSender,
        IShorfahUrlProvider urlProvider,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
        _notificationSender = notificationSender;
        _urlProvider = urlProvider;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(SendShorfahSectionReminderCommand request, CancellationToken cancellationToken)
    {
        ShorfahSection? section = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahSections.Where(s => s.Id == request.SectionId), cancellationToken);
        if (section is null)
        {
            return Result<bool>.Failure("القسم غير موجود");
        }

        ShorfahIssue? issue = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahIssues.Where(i => i.Id == section.IssueId), cancellationToken);
        User? recipient = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Users.Where(u => u.Id == request.UserId), cancellationToken);

        ReminderMessage message = BuildMessage(section, issue, recipient);
        await PersistAndSendAsync(request, section, recipient, message, cancellationToken);

        return Result<bool>.Success(true);
    }

    private ReminderMessage BuildMessage(ShorfahSection section, ShorfahIssue? issue, User? recipient)
    {
        DateTimeOffset now = _dateTimeProvider.RiyadhNow;
        var daysOverdue = section.SlaDeadline.HasValue ? Math.Max(0, (int)(now - section.SlaDeadline.Value).TotalDays) : 0;
        var recipientName = recipient?.Name ?? "المساهم";
        var relativeUrl = $"/#/shorfah/{section.IssueId}";
        var absoluteUrl = $"{_urlProvider.FrontendBaseUrl}{relativeUrl}";
        var title = daysOverdue > 0
            ? $"تذكير: قسم \"{section.TitleAr}\" متأخر {daysOverdue} يوم"
            : $"تذكير: قسم \"{section.TitleAr}\" قيد التجميع";
        var emailHtml = ShorfahNotificationHtmlBuilder.BuildOverdue(recipientName, section.TitleAr, issue?.TitleAr ?? "شرفة", daysOverdue, absoluteUrl);
        var body = $"يُرجى تسليم المحتوى الخاص بك لقسم \"{section.TitleAr}\" في أقرب وقت.";

        return new ReminderMessage(title, body, relativeUrl, emailHtml, daysOverdue, now);
    }

    private async Task PersistAndSendAsync(
        SendShorfahSectionReminderCommand request, ShorfahSection section, User? recipient, ReminderMessage message, CancellationToken cancellationToken)
    {
        _dbContext.Add(new ShorfahNotification
        {
            UserId = request.UserId,
            IssueId = section.IssueId,
            SectionId = section.Id,
            Type = "reminder_overdue",
            Title = message.Title,
            Body = message.Body,
            Url = message.RelativeUrl,
            IsRead = false,
            CreatedBy = ShorfahMappers.IdString(request.ActorUserId),
        });

        _dbContext.Add(new ShorfahReminder
        {
            SectionId = section.Id,
            RecipientUserId = request.UserId,
            Channel = recipient?.Email is null ? ShorfahReminderChannel.InApp : ShorfahReminderChannel.Both,
            ReminderType = ShorfahReminderType.PreDue,
            SentAt = message.SentAt,
            Status = "sent",
            Message = message.Title,
            CreatedBy = ShorfahMappers.IdString(request.ActorUserId),
        });

        await _notificationSender.SendEmailAsync(recipient?.Email, message.Title, message.EmailHtml, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_section.remind",
            "ShorfahSection",
            ShorfahMappers.IdString(section.Id),
            before: null,
            after: new { request.UserId, daysOverdue = message.DaysOverdue },
            cancellationToken);
    }

    /// <summary>The rendered reminder content and delivery metadata built once and shared by the notification row, reminder row, and outbound email.</summary>
    private sealed record ReminderMessage(string Title, string Body, string RelativeUrl, string EmailHtml, int DaysOverdue, DateTimeOffset SentAt);
}
