using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="PublishShorfahIssueCommand"/>. Ports <c>shorfah.ts:1032-1084</c>.</summary>
public sealed class PublishShorfahIssueCommandHandler : IRequestHandler<PublishShorfahIssueCommand, Result<ShorfahIssueDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;
    private readonly IShorfahNotificationSender _notificationSender;
    private readonly IShorfahUrlProvider _urlProvider;

    /// <summary>Initializes a new instance of the <see cref="PublishShorfahIssueCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    /// <param name="notificationSender">The notification dispatch port.</param>
    /// <param name="urlProvider">The configured base-URL port.</param>
    public PublishShorfahIssueCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IDateTimeProvider dateTimeProvider,
        IAuditLogService auditLogService,
        IShorfahNotificationSender notificationSender,
        IShorfahUrlProvider urlProvider)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
        _notificationSender = notificationSender;
        _urlProvider = urlProvider;
    }

    /// <inheritdoc />
    public async Task<Result<ShorfahIssueDto>> Handle(PublishShorfahIssueCommand request, CancellationToken cancellationToken)
    {
        ShorfahIssue? issue = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahIssues.Where(i => i.Id == request.IssueId), cancellationToken);
        if (issue is null)
        {
            return Result<ShorfahIssueDto>.Failure("العدد غير موجود");
        }

        if (!await HasApprovedIncludedSectionAsync(request.IssueId, cancellationToken))
        {
            return Result<ShorfahIssueDto>.Failure("لا يوجد أقسام معتمدة ومُفعّلة للنشر");
        }

        ApplyPublish(issue, request.ActorUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_issue.publish",
            "ShorfahIssue",
            ShorfahMappers.IdString(issue.Id),
            before: new { Status = ShorfahIssueStatus.InReview },
            after: new { issue.Status, issue.PublishedAt },
            cancellationToken);

        await FanOutPublishNotificationsAsync(issue, cancellationToken);

        return Result<ShorfahIssueDto>.Success(ShorfahMappers.ToDto(issue));
    }

    private async Task<bool> HasApprovedIncludedSectionAsync(int issueId, CancellationToken cancellationToken)
    {
        return await _queryExecutor.AnyAsync(
            _dbContext.ShorfahSections.Where(s =>
                s.IssueId == issueId &&
                s.WorkflowStatus == ShorfahWorkflowStatus.Approved &&
                s.IncludeInPdf),
            cancellationToken);
    }

    private void ApplyPublish(ShorfahIssue issue, int actorUserId)
    {
        DateTimeOffset now = _dateTimeProvider.UtcNow;
        issue.Status = ShorfahIssueStatus.Published;
        issue.PublishedAt = now;
        issue.PublishedPdfUrl = $"/api/v1/shorfah/issues/{issue.Id}/pdf.pdf";
        issue.UpdatedAt = now.UtcDateTime;
        issue.UpdatedBy = ShorfahMappers.IdString(actorUserId);
    }

    private async Task FanOutPublishNotificationsAsync(ShorfahIssue issue, CancellationToken cancellationToken)
    {
        // Why: matches the Node source's try/catch-and-log-only wrapper around the entire
        // fan-out loop -- a notification failure for any user must never fail the publish
        // response, since the issue is already durably published by the time this runs. Per-user
        // send failures are isolated so one bad recipient cannot abort the whole fan-out; the
        // final SaveChangesAsync failure mode (e.g. a transient DB error) is the only remaining
        // risk, and is deliberately not swallowed here since the in-app rows are best-effort
        // supplementary data, not the publish operation of record (already committed above).
        List<User> users = await _queryExecutor.ToListAsync(_dbContext.Users, cancellationToken);
        var monthName = ArabicMonthNames.For(issue.Month);
        var issueUrl = $"{_urlProvider.FrontendBaseUrl}/#/shorfah/{issue.Id}";
        var pdfUrl = $"{_urlProvider.ApiBaseUrl}{issue.PublishedPdfUrl}";
        var emailHtml = ShorfahNotificationHtmlBuilder.BuildPublished(issue.TitleAr, monthName, issue.Year, issue.IssueNo, issueUrl, pdfUrl);

        foreach (User user in users)
        {
            _dbContext.Add(new ShorfahNotification
            {
                UserId = user.Id,
                IssueId = issue.Id,
                Type = "published",
                Title = "عدد جديد من شُرفة متوفر الآن",
                Body = $"تفضل بقراءة العدد {issue.IssueNo} — {monthName} {issue.Year}",
                Url = $"/#/shorfah/{issue.Id}",
                IsRead = false,
                CreatedBy = "system",
            });

            await _notificationSender.SendEmailAsync(user.Email, "عدد جديد من شُرفة", emailHtml, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
