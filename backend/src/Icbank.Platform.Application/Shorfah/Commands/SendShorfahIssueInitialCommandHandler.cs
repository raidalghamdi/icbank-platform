using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="SendShorfahIssueInitialCommand"/>. Ports <c>shorfah.ts:890-954</c>.</summary>
public sealed class SendShorfahIssueInitialCommandHandler : IRequestHandler<SendShorfahIssueInitialCommand, Result<SendShorfahIssueInitialResultDto>>
{
    /// <summary>The sentinel error message the controller maps to 429, distinguishing it from the 404 "issue not found" failure.</summary>
    public const string RateLimitedError = "تم تجاوز حد الإرسال المؤقت، انتظر قليلاً وحاول مجدداً.";

    private const int DefaultSlaDays = 7;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;
    private readonly IShorfahNotificationSender _notificationSender;
    private readonly IShorfahUrlProvider _urlProvider;
    private readonly IShorfahSendInitialRateLimiter _rateLimiter;

    /// <summary>Initializes a new instance of the <see cref="SendShorfahIssueInitialCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    /// <param name="notificationSender">The notification dispatch port.</param>
    /// <param name="urlProvider">The configured base-URL port.</param>
    /// <param name="rateLimiter">The send-initial rate limiter.</param>
    public SendShorfahIssueInitialCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IDateTimeProvider dateTimeProvider,
        IAuditLogService auditLogService,
        IShorfahNotificationSender notificationSender,
        IShorfahUrlProvider urlProvider,
        IShorfahSendInitialRateLimiter rateLimiter)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
        _notificationSender = notificationSender;
        _urlProvider = urlProvider;
        _rateLimiter = rateLimiter;
    }

    /// <inheritdoc />
    public async Task<Result<SendShorfahIssueInitialResultDto>> Handle(SendShorfahIssueInitialCommand request, CancellationToken cancellationToken)
    {
        if (!_rateLimiter.TryConsume(request.ActorUserId))
        {
            return Result<SendShorfahIssueInitialResultDto>.Failure(RateLimitedError);
        }

        ShorfahIssue? issue = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahIssues.Where(i => i.Id == request.IssueId), cancellationToken);
        if (issue is null)
        {
            return Result<SendShorfahIssueInitialResultDto>.Failure("العدد غير موجود");
        }

        List<SendShorfahIssueInitialEntryDto> results = await ProcessIssueSectionsAsync(issue, request.ActorUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_issue.send_initial",
            "ShorfahIssue",
            ShorfahMappers.IdString(issue.Id),
            before: null,
            after: new { sent = results.Count },
            cancellationToken);

        return Result<SendShorfahIssueInitialResultDto>.Success(new SendShorfahIssueInitialResultDto(results.Count, results));
    }

    private static DateTimeOffset StartSlaClock(ShorfahSection section, DateTimeOffset now, int actorUserId)
    {
        var slaDays = section.SlaDays ?? DefaultSlaDays;
        DateTimeOffset deadline = now.AddDays(slaDays);
        section.SlaStartsAt = now;
        section.SlaDeadline = deadline;
        section.UpdatedAt = now.UtcDateTime;
        section.UpdatedBy = ShorfahMappers.IdString(actorUserId);
        return deadline;
    }

    private async Task<List<SendShorfahIssueInitialEntryDto>> ProcessIssueSectionsAsync(ShorfahIssue issue, int actorUserId, CancellationToken cancellationToken)
    {
        List<ShorfahSection> sections = await _queryExecutor.ToListAsync(
            _dbContext.ShorfahSections.Where(s => s.IssueId == issue.Id), cancellationToken);
        var sectionIds = sections.Select(s => s.Id).ToList();
        List<ShorfahAssignment> assignments = await _queryExecutor.ToListAsync(
            _dbContext.ShorfahAssignments.Where(a => sectionIds.Contains(a.SectionId)), cancellationToken);
        var usersById = (await _queryExecutor.ToListAsync(_dbContext.Users, cancellationToken)).ToDictionary(u => u.Id);

        DateTimeOffset now = _dateTimeProvider.UtcNow;
        var results = new List<SendShorfahIssueInitialEntryDto>();

        foreach (ShorfahSection section in sections)
        {
            DateTimeOffset deadline = StartSlaClock(section, now, actorUserId);
            IEnumerable<ShorfahAssignment> sectionAssignments = assignments.Where(a => a.SectionId == section.Id);
            foreach (ShorfahAssignment assignment in sectionAssignments)
            {
                await NotifyAssignmentAsync(issue, section, assignment, deadline, usersById, actorUserId, cancellationToken);
                results.Add(new SendShorfahIssueInitialEntryDto(section.Id, assignment.UserId, "sent"));
            }
        }

        return results;
    }

    private async Task NotifyAssignmentAsync(
        ShorfahIssue issue,
        ShorfahSection section,
        ShorfahAssignment assignment,
        DateTimeOffset deadline,
        Dictionary<int, User> usersById,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        usersById.TryGetValue(assignment.UserId, out User? recipient);
        var recipientName = recipient?.Name ?? "المساهم";
        var monthName = ArabicMonthNames.For(issue.Month);
        var deadlineStr = deadline.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var relativeUrl = $"/#/shorfah/{issue.Id}";
        var absoluteUrl = $"{_urlProvider.FrontendBaseUrl}{relativeUrl}";
        var subject = $"مطلوب مساهمتك في شُرفة — {section.TitleAr}";
        var emailHtml = ShorfahNotificationHtmlBuilder.BuildInitial(recipientName, section.TitleAr, issue.TitleAr, deadlineStr, absoluteUrl);

        _dbContext.Add(new ShorfahNotification
        {
            UserId = assignment.UserId,
            IssueId = issue.Id,
            SectionId = section.Id,
            Type = "initial",
            Title = subject,
            Body = $"تمت دعوتك للمساهمة في قسم \"{section.TitleAr}\" من عدد \"{issue.TitleAr}\" ({monthName} {issue.Year}). آخر موعد: {deadlineStr}",
            Url = relativeUrl,
            IsRead = false,
            CreatedBy = ShorfahMappers.IdString(actorUserId),
        });

        await _notificationSender.SendEmailAsync(recipient?.Email, subject, emailHtml, cancellationToken);
    }
}
