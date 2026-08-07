using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Gac;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Handles <see cref="GenerateMediaReportCommand"/>. Ports BUSINESS-RULES.md §5.1's pipeline: (1)
/// resolve the audience/type/date-range with the Node source's exact 7-day/30-day defaults, (2)
/// pull cached social posts and news items in range, (3) if zero source items exist, skip the AI
/// call entirely and produce a canned "no data" message in code (avoiding a wasted AI call on
/// empty input, matching the Node source precisely), otherwise (4) delegate the narrative to
/// <see cref="IMediaReportNarrativeGenerator"/> and persist.
/// </summary>
public sealed class GenerateMediaReportCommandHandler : IRequestHandler<GenerateMediaReportCommand, Result<MediaReportDto>>
{
    private const int WeeklyRangeDays = 7;
    private const int MonthlyRangeDays = 30;
    private const string NoDataMessage = "## لا توجد بيانات\n\nلم يتم رصد أي منشورات أو أخبار خلال الفترة المحددة.";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMediaReportNarrativeGenerator _narrativeGenerator;

    /// <summary>Initializes a new instance of the <see cref="GenerateMediaReportCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    /// <param name="dateTimeProvider">The Riyadh-aware clock port.</param>
    /// <param name="narrativeGenerator">The report-narrative generation port.</param>
    public GenerateMediaReportCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IMediaReportNarrativeGenerator narrativeGenerator)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _narrativeGenerator = narrativeGenerator;
    }

    /// <inheritdoc />
    public async Task<Result<MediaReportDto>> Handle(GenerateMediaReportCommand request, CancellationToken cancellationToken)
    {
        MediaReportType reportType = ParseReportType(request.ReportType);
        MediaReportAudience audience = ParseAudience(request.Audience);
        (DateTimeOffset dateFrom, DateTimeOffset dateTo) = ResolveRange(request, reportType);

        List<GacSocialPost> posts = await _queryExecutor.ToListAsync(
            _dbContext.GacSocialPosts.Where(p => p.PostedAt >= dateFrom && p.PostedAt <= dateTo), cancellationToken);
        List<GacNewsItem> news = await _queryExecutor.ToListAsync(
            _dbContext.GacNewsItems.Where(n => n.PublishedAt >= dateFrom && n.PublishedAt <= dateTo), cancellationToken);

        (string ContentMd, string? ExecutiveSummary, string? OverallTone) narrative =
            await BuildNarrativeAsync(audience, posts, news, cancellationToken);

        MediaReport report = BuildReport(request, reportType, audience, dateFrom, dateTo, posts, news, narrative);

        _dbContext.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "media_report.generate",
            "MediaReport",
            report.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { report.Title, report.Audience },
            cancellationToken);

        return Result<MediaReportDto>.Success(MediaReportMapper.ToDto(report));
    }

    private static MediaReport BuildReport(
        GenerateMediaReportCommand request,
        MediaReportType reportType,
        MediaReportAudience audience,
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        List<GacSocialPost> posts,
        List<GacNewsItem> news,
        (string ContentMd, string? ExecutiveSummary, string? OverallTone) narrative)
    {
        IReadOnlyList<string> sources = request.Sources?.Count > 0 ? request.Sources : new[] { "linkedin", "news" };
        return new MediaReport
        {
            Title = request.CustomTitle ?? BuildDefaultTitle(reportType, dateFrom, dateTo),
            ReportType = reportType,
            Audience = audience,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Sources = sources.ToList(),
            ExecutiveSummary = narrative.ExecutiveSummary,
            ContentMd = narrative.ContentMd,
            OverallTone = narrative.OverallTone,
            SourceItemsJson = System.Text.Json.JsonSerializer.Serialize(new { postCount = posts.Count, newsCount = news.Count }),
            GeneratedByUserId = request.ActorUserId,
            Status = MediaReportStatus.Published,
        };
    }

    private static MediaReportType ParseReportType(string? reportType) =>
        Enum.TryParse(reportType, ignoreCase: true, out MediaReportType parsed) ? parsed : MediaReportType.Weekly;

    private static MediaReportAudience ParseAudience(string? audience) =>
        Enum.TryParse(audience, ignoreCase: true, out MediaReportAudience parsed) ? parsed : MediaReportAudience.Manager;

    private static string BuildDefaultTitle(MediaReportType reportType, DateTimeOffset dateFrom, DateTimeOffset dateTo) =>
        $"تقرير رصد إعلامي {reportType} ({dateFrom:yyyy-MM-dd} - {dateTo:yyyy-MM-dd})";

    private async Task<(string ContentMd, string? ExecutiveSummary, string? OverallTone)> BuildNarrativeAsync(
        MediaReportAudience audience, List<GacSocialPost> posts, List<GacNewsItem> news, CancellationToken cancellationToken)
    {
        if (posts.Count == 0 && news.Count == 0)
        {
            // Why: BUSINESS-RULES.md §5.1 -- no AI call is made on empty input, avoiding wasted cost.
            return (NoDataMessage, null, null);
        }

        var feed = SourceFeedFormatter.Format(posts, news);
        MediaReportNarrative narrative = await _narrativeGenerator.GenerateAsync(audience.ToString().ToLowerInvariant(), feed, cancellationToken);
        return (narrative.ContentMd, narrative.ExecutiveSummary, narrative.OverallTone);
    }

    private (DateTimeOffset DateFrom, DateTimeOffset DateTo) ResolveRange(GenerateMediaReportCommand request, MediaReportType reportType)
    {
        DateTimeOffset now = _dateTimeProvider.RiyadhNow;
        if (request.DateFrom.HasValue && request.DateTo.HasValue)
        {
            return (request.DateFrom.Value, request.DateTo.Value);
        }

        var days = reportType == MediaReportType.Monthly ? MonthlyRangeDays : WeeklyRangeDays;
        return (now.AddDays(-days), now);
    }
}
