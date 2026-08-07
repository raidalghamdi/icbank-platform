using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="CreateFinalMediaReportCommand"/>. Computes the report number and content hash, then persists a permanently-immutable row.</summary>
public sealed class CreateFinalMediaReportCommandHandler : IRequestHandler<CreateFinalMediaReportCommand, Result<FinalMediaReportDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new instance of the <see cref="CreateFinalMediaReportCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    /// <param name="dateTimeProvider">The Riyadh-aware clock port.</param>
    public CreateFinalMediaReportCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<FinalMediaReportDto>> Handle(CreateFinalMediaReportCommand request, CancellationToken cancellationToken)
    {
        DateTimeOffset now = _dateTimeProvider.RiyadhNow;
        List<string> existingNumbers = await _queryExecutor.ToListAsync(_dbContext.FinalMediaReports.Select(r => r.ReportNumber), cancellationToken);
        var reportNumber = FinalReportNumberGenerator.Next(existingNumbers, now.Year);

        FinalMediaReport report = BuildReport(request, reportNumber, now);
        _dbContext.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "final_media_report.create",
            "FinalMediaReport",
            report.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { report.ReportNumber, report.Title },
            cancellationToken);

        return Result<FinalMediaReportDto>.Success(FinalMediaReportMapper.ToSummaryDto(report));
    }

    private static FinalMediaReport BuildReport(CreateFinalMediaReportCommand request, string reportNumber, DateTimeOffset now)
    {
        MediaReportType reportType = Enum.TryParse(request.ReportType, ignoreCase: true, out MediaReportType parsed) ? parsed : MediaReportType.Weekly;
        FinalReportDraftDto draft = request.Draft;

        return new FinalMediaReport
        {
            ReportNumber = reportNumber,
            Title = request.Title,
            ReportType = reportType,
            PeriodLabel = request.PeriodLabel,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            IssueDate = now,
            ExecutiveSummary = draft.ExecutiveSummary,
            Kpis = new ReportKpis { TotalNews = draft.Kpis.TotalNews, PositivePercent = draft.Kpis.PositivePercent, MediaOutlets = draft.Kpis.MediaOutlets, KeyTopics = draft.Kpis.KeyTopics, Reach = draft.Kpis.Reach, AlertsCount = draft.Kpis.AlertsCount },
            TopNews = draft.TopNews.Select(n => new TopNewsItem { Date = n.Date, Tone = n.Tone, Headline = n.Headline, Details = n.Details.ToList(), Source = n.Source }).ToList(),
            Timeline = draft.Timeline.Select(t => new TimelineEvent { Date = t.Date, Event = t.Event, Outlet = t.Outlet, Tone = t.Tone, Count = t.Count }).ToList(),
            DigitalPresence = BuildDigitalPresence(draft.DigitalPresence),
            EditorialTone = BuildEditorialTone(draft.EditorialTone),
            DeepAnalysis = BuildDeepAnalysis(draft.DeepAnalysis),
            RegionalComparison = draft.RegionalComparison.Select(r => new RegionalComparison { Authority = r.Authority, Country = r.Country, Mentions = r.Mentions, Tone = r.Tone, Highlights = r.Highlights }).ToList(),
            Recommendations = draft.Recommendations.Select(r => new Recommendation { Title = r.Title, Description = r.Description, Priority = r.Priority, Responsible = r.Responsible, Kpi = r.Kpi, Deadline = r.Deadline, Dependencies = r.Dependencies }).ToList(),
            Alerts = draft.Alerts.Select(a => new AlertItem { Alert = a.Alert, SuggestedPosition = a.SuggestedPosition }).ToList(),
            QuotesAppendix = draft.QuotesAppendix.Select(q => new QuoteAppendixItem { Quote = q.Quote, Source = q.Source, Date = q.Date, Topic = q.Topic }).ToList(),
            Methodology = draft.Methodology,
            Sources = draft.Sources.Select(s => new SourceRef { Name = s.Name, Url = s.Url, Description = s.Description }).ToList(),
            SourceItemsJson = "[]",
            GeneratedByUserId = request.ActorUserId,
            Status = FinalMediaReportStatus.Final,
            LockedAt = now,
            ContentSha256 = FinalReportContentHasher.ComputeSha256(draft),
        };
    }

    private static DigitalPresence BuildDigitalPresence(DigitalPresenceDto dto) => new()
    {
        Platforms = dto.Platforms.Select(p => new DigitalPresencePlatform { Name = p.Name, Mentions = p.Mentions, Reposts = p.Reposts, Engagement = p.Engagement, Reach = p.Reach }).ToList(),
        Hashtags = dto.Hashtags.Select(h => new DigitalPresenceHashtag { Tag = h.Tag, Uses = h.Uses, Trend = h.Trend }).ToList(),
    };

    private static EditorialTone BuildEditorialTone(EditorialToneDto dto) => new()
    {
        Distribution = dto.Distribution.Select(ToBucket).ToList(),
        Classification = dto.Classification.Select(ToBucket).ToList(),
        Sources = dto.Sources.Select(ToBucket).ToList(),
    };

    private static EditorialToneBucket ToBucket(EditorialToneBucketDto dto) => new() { Label = dto.Label, Percent = dto.Percent, Count = dto.Count };

    private static DeepAnalysis BuildDeepAnalysis(DeepAnalysisDto dto) => new()
    {
        Keywords = dto.Keywords.Select(k => new DeepAnalysisKeyword { Keyword = k.Keyword, Frequency = k.Frequency, Context = k.Context }).ToList(),
        Quote = dto.Quote is null ? null : new DeepAnalysisQuote { Text = dto.Quote.Text, Source = dto.Quote.Source, Date = dto.Quote.Date },
        Strengths = dto.Strengths.ToList(),
        Weaknesses = dto.Weaknesses.ToList(),
    };
}
