using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Handles <see cref="CheckShorfahOverdueSectionsCommand"/>. Ports <c>shorfah-cron.ts:21-84</c>
/// with idempotent, same-Riyadh-day dedup added (BUSINESS-RULES.md §1.6, AMBIGUOUS-BR-2 resolved
/// in favor of a cooldown): before sending, each section/recipient pair is checked against
/// <see cref="ShorfahReminder"/> rows already written today for an <see cref="ShorfahReminderType.Overdue"/>
/// reminder; if one exists, that recipient is skipped this run.
/// </summary>
public sealed class CheckShorfahOverdueSectionsCommandHandler : IRequestHandler<CheckShorfahOverdueSectionsCommand, Result<CheckShorfahOverdueSectionsResultDto>>
{
    private static readonly ShorfahWorkflowStatus[] OverdueEligibleStatuses =
    {
        ShorfahWorkflowStatus.PendingContribution, ShorfahWorkflowStatus.Submitted,
    };

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IShorfahNotificationSender _notificationSender;
    private readonly IShorfahUrlProvider _urlProvider;

    /// <summary>Initializes a new instance of the <see cref="CheckShorfahOverdueSectionsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock, resolved in Asia/Riyadh for the dedup-day boundary.</param>
    /// <param name="notificationSender">The notification dispatch port.</param>
    /// <param name="urlProvider">The configured base-URL port.</param>
    /// <remarks>
    /// No <see cref="IAuditLogService"/> dependency: the cron caller has no <c>User</c> row (the
    /// audit schema's <c>ActorUserId</c> is a real, enforced foreign key -- DOTNET-CONVENTIONS.md
    /// §5.5), so a system/service actor cannot write to that table without either a synthetic
    /// seeded system user (a cross-cutting decision out of scope for this wave) or relaxing the FK
    /// (a regression). The <see cref="ShorfahNotification"/>/<see cref="ShorfahReminder"/> rows this
    /// handler writes are themselves a complete, queryable trail of every action the cron took.
    /// </remarks>
    public CheckShorfahOverdueSectionsCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IDateTimeProvider dateTimeProvider,
        IShorfahNotificationSender notificationSender,
        IShorfahUrlProvider urlProvider)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
        _notificationSender = notificationSender;
        _urlProvider = urlProvider;
    }

    /// <inheritdoc />
    public async Task<Result<CheckShorfahOverdueSectionsResultDto>> Handle(CheckShorfahOverdueSectionsCommand request, CancellationToken cancellationToken)
    {
        DateTimeOffset riyadhNow = _dateTimeProvider.RiyadhNow;
        DateTimeOffset utcNow = _dateTimeProvider.UtcNow;
        DateTime riyadhToday = riyadhNow.Date;

        List<ShorfahSection> overdueSections = await FindOverdueSectionsAsync(utcNow, cancellationToken);
        if (overdueSections.Count == 0)
        {
            return Result<CheckShorfahOverdueSectionsResultDto>.Success(new CheckShorfahOverdueSectionsResultDto(0, 0));
        }

        OverdueContext context = await LoadContextAsync(overdueSections, riyadhToday, cancellationToken);
        var notified = await NotifyOverdueAssigneesAsync(overdueSections, context, utcNow, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<CheckShorfahOverdueSectionsResultDto>.Success(new CheckShorfahOverdueSectionsResultDto(overdueSections.Count, notified));
    }

    private async Task<OverdueContext> LoadContextAsync(List<ShorfahSection> overdueSections, DateTime riyadhToday, CancellationToken cancellationToken)
    {
        var sectionIds = overdueSections.Select(s => s.Id).ToList();
        var issuesById = (await _queryExecutor.ToListAsync(
            _dbContext.ShorfahIssues.Where(i => overdueSections.Select(s => s.IssueId).Contains(i.Id)), cancellationToken)).ToDictionary(i => i.Id);
        List<ShorfahAssignment> assignments = await _queryExecutor.ToListAsync(
            _dbContext.ShorfahAssignments.Where(a => sectionIds.Contains(a.SectionId)), cancellationToken);
        var usersById = (await _queryExecutor.ToListAsync(_dbContext.Users, cancellationToken)).ToDictionary(u => u.Id);
        HashSet<(int SectionId, int UserId)> alreadyRemindedToday = await AlreadyRemindedTodayAsync(sectionIds, riyadhToday, cancellationToken);

        return new OverdueContext(issuesById, assignments, usersById, alreadyRemindedToday);
    }

    private async Task<int> NotifyOverdueAssigneesAsync(
        List<ShorfahSection> overdueSections, OverdueContext context, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var notified = 0;
        foreach (ShorfahSection section in overdueSections)
        {
            context.IssuesById.TryGetValue(section.IssueId, out ShorfahIssue? issue);
            foreach (ShorfahAssignment assignment in context.Assignments.Where(a => a.SectionId == section.Id))
            {
                if (!context.AlreadyRemindedToday.Add((section.Id, assignment.UserId)))
                {
                    continue;
                }

                context.UsersById.TryGetValue(assignment.UserId, out User? recipient);
                await NotifyAsync(section, issue, assignment, recipient, utcNow, cancellationToken);
                notified++;
            }
        }

        return notified;
    }

    private async Task<List<ShorfahSection>> FindOverdueSectionsAsync(DateTimeOffset utcNow, CancellationToken cancellationToken) =>
        await _queryExecutor.ToListAsync(
            _dbContext.ShorfahSections.Where(s =>
                OverdueEligibleStatuses.Contains(s.WorkflowStatus) && s.SlaDeadline.HasValue && s.SlaDeadline.Value < utcNow),
            cancellationToken);

    private async Task<HashSet<(int SectionId, int UserId)>> AlreadyRemindedTodayAsync(
        List<int> sectionIds, DateTime riyadhToday, CancellationToken cancellationToken)
    {
        List<ShorfahReminder> todaysReminders = await _queryExecutor.ToListAsync(
            _dbContext.ShorfahReminders.Where(r =>
                sectionIds.Contains(r.SectionId) && r.ReminderType == ShorfahReminderType.Overdue && r.SentAt.HasValue),
            cancellationToken);

        return todaysReminders
            .Where(r => r.SentAt!.Value.Date == riyadhToday)
            .Select(r => (r.SectionId, r.RecipientUserId))
            .ToHashSet();
    }

    private async Task NotifyAsync(
        ShorfahSection section, ShorfahIssue? issue, ShorfahAssignment assignment, User? recipient, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var daysOverdue = section.SlaDeadline.HasValue ? Math.Max(0, (int)(utcNow - section.SlaDeadline.Value).TotalDays) : 0;
        var relativeUrl = $"/#/shorfah/{section.IssueId}";
        var absoluteUrl = $"{_urlProvider.FrontendBaseUrl}{relativeUrl}";
        var title = $"قسم \"{section.TitleAr}\" متأخر عن الموعد بـ {daysOverdue} يوم";
        var recipientName = recipient?.Name ?? "المساهم";
        var emailHtml = ShorfahNotificationHtmlBuilder.BuildOverdue(recipientName, section.TitleAr, issue?.TitleAr ?? "شرفة", daysOverdue, absoluteUrl);

        _dbContext.Add(new ShorfahNotification
        {
            UserId = assignment.UserId,
            IssueId = section.IssueId,
            SectionId = section.Id,
            Type = "reminder_overdue",
            Title = title,
            Body = "يُرجى تسليم المحتوى الخاص بك في أقرب وقت ممكن.",
            Url = relativeUrl,
            IsRead = false,
            CreatedBy = "cron",
        });

        _dbContext.Add(new ShorfahReminder
        {
            SectionId = section.Id,
            AssignmentId = assignment.Id,
            RecipientUserId = assignment.UserId,
            Channel = recipient?.Email is null ? ShorfahReminderChannel.InApp : ShorfahReminderChannel.Both,
            ReminderType = ShorfahReminderType.Overdue,
            SentAt = utcNow,
            Status = "sent",
            Message = title,
            CreatedBy = "cron",
        });

        await _notificationSender.SendEmailAsync(recipient?.Email, title, emailHtml, cancellationToken);
    }

    /// <summary>The per-run lookup data needed to notify overdue-section assignees.</summary>
    private sealed record OverdueContext(
        Dictionary<int, ShorfahIssue> IssuesById,
        List<ShorfahAssignment> Assignments,
        Dictionary<int, User> UsersById,
        HashSet<(int SectionId, int UserId)> AlreadyRemindedToday);
}
