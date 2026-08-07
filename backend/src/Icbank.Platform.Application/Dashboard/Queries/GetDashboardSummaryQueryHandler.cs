using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.AiYear;
using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Dashboard.Queries;

/// <summary>
/// Handles <see cref="GetDashboardSummaryQuery"/>. Ports BUSINESS-RULES.md §9's aggregation rules
/// with two deliberate behaviour fixes over the Node source: (1) the international-days lookup no
/// longer silently truncates at 300 rows (closes BUG-03 — reads the full catalogue since it is a
/// bounded reference table, not a growth-unbounded business table); (2) "this month" for
/// Week-Start entries is computed against <see cref="IDateTimeProvider.RiyadhNow"/> instead of
/// naive server-local time (closes the timezone gap BUSINESS-RULES.md §2.1/§9 flags).
/// </summary>
public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private const int RecentActivationCount = 5;
    private const int ArchiveSampleSize = 50;
    private const int UpcomingWindowDays = 30;
    private const int UpcomingDisplayCount = 3;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new instance of the <see cref="GetDashboardSummaryQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock, resolved to Asia/Riyadh local time for "this month" comparisons.</param>
    public GetDashboardSummaryQueryHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        DateTimeOffset riyadhNow = _dateTimeProvider.RiyadhNow;

        List<AiYearActivation> totalActivations = await _queryExecutor.ToListAsync(_dbContext.AiYearActivations, cancellationToken);
        var recentActivations = totalActivations
            .OrderByDescending(a => a.CreatedAt)
            .Take(RecentActivationCount)
            .ToList();

        var archiveSample = (await _queryExecutor.ToListAsync(_dbContext.ArchiveEntries, cancellationToken))
            .OrderByDescending(e => e.CreatedAt)
            .Take(ArchiveSampleSize)
            .ToList();
        var archiveTotal = (await _queryExecutor.ToListAsync(_dbContext.ArchiveEntries, cancellationToken)).Count;

        List<InternationalDay> internationalDays = await _queryExecutor.ToListAsync(_dbContext.InternationalDays, cancellationToken);

        List<UpcomingInternationalDayDto> upcoming = ComputeUpcomingDays(internationalDays, riyadhNow);
        var thisMonthCount = archiveSample.Count(e => IsInRiyadhMonth(e.CreatedAt, riyadhNow));

        var summary = new DashboardSummaryDto(
            new DashboardKpiDto(totalActivations.Count, thisMonthCount, archiveTotal, upcoming.Count),
            new WeekStartSummaryDto(thisMonthCount, archiveTotal, archiveSample.FirstOrDefault()?.Title),
            new AiYearSummaryDto(totalActivations.Count, recentActivations.Select(ToRecentActivationDto).ToList()),
            upcoming.Take(UpcomingDisplayCount).ToList());

        return Result<DashboardSummaryDto>.Success(summary);
    }

    private static bool IsInRiyadhMonth(DateTime createdAtUtc, DateTimeOffset riyadhNow)
    {
        // Why: CreatedAt is stored as UTC (AuditableEntity, R-BE-026); comparing calendar months
        // must happen in Riyadh local time, not the raw UTC value, to close the timezone gap.
        var createdAtUtcOffset = new DateTimeOffset(createdAtUtc, TimeSpan.Zero);
        DateTimeOffset createdAtRiyadh = createdAtUtcOffset.ToOffset(riyadhNow.Offset);
        return createdAtRiyadh.Month == riyadhNow.Month && createdAtRiyadh.Year == riyadhNow.Year;
    }

    private static List<UpcomingInternationalDayDto> ComputeUpcomingDays(
        IReadOnlyList<InternationalDay> days, DateTimeOffset riyadhNow)
    {
        DateTime todayStart = riyadhNow.Date;
        var results = new List<UpcomingInternationalDayDto>();

        foreach (InternationalDay day in days)
        {
            if (string.IsNullOrWhiteSpace(day.AnnualDate))
            {
                continue;
            }

            (int Month, int Day)? parsed = ArabicAnnualDateParser.Parse(day.AnnualDate);
            if (parsed is null)
            {
                continue;
            }

            DateTime target = ResolveNextOccurrence(parsed.Value.Month, parsed.Value.Day, todayStart);
            var daysUntil = (int)Math.Round((target - todayStart).TotalDays);
            if (daysUntil is >= 0 and <= UpcomingWindowDays)
            {
                results.Add(new UpcomingInternationalDayDto(day.Id, day.DayNameAr, target.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), daysUntil, day.Category));
            }
        }

        return results.OrderBy(d => d.DaysUntil).ToList();
    }

    private static DateTime ResolveNextOccurrence(int month, int day, DateTime todayStart)
    {
        try
        {
            var thisYear = new DateTime(todayStart.Year, month, day);
            return thisYear >= todayStart ? thisYear : new DateTime(todayStart.Year + 1, month, day);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Why: source data can contain an invalid day-of-month (e.g. Feb 30); skip rather
            // than throw, matching the Node parser's silent-skip behavior for malformed input.
            return todayStart.AddYears(100);
        }
    }

    private static RecentActivationDto ToRecentActivationDto(AiYearActivation activation) => new(
        activation.Id, activation.Title, activation.Type, activation.Status.ToString(), activation.ActivationDate, activation.CreatedAt);
}
