using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Gac;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Handles <see cref="GenerateFinalMediaReportCommand"/>. Ports BUSINESS-RULES.md §5.3's
/// no-source-data guard exactly: if the requested range has zero posts and zero news, no AI call
/// is made and a detailed <c>NO_SOURCE_DATA</c> diagnostic is returned instead (including the
/// actual available date range across all historical data), rather than a generic empty result.
/// </summary>
public sealed class GenerateFinalMediaReportCommandHandler : IRequestHandler<GenerateFinalMediaReportCommand, Result<GenerateFinalMediaReportResultDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;
    private readonly IFinalReportSectionGenerator _sectionGenerator;

    /// <summary>Initializes a new instance of the <see cref="GenerateFinalMediaReportCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    /// <param name="sectionGenerator">The 8-section content generation port.</param>
    public GenerateFinalMediaReportCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService, IFinalReportSectionGenerator sectionGenerator)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
        _sectionGenerator = sectionGenerator;
    }

    /// <inheritdoc />
    public async Task<Result<GenerateFinalMediaReportResultDto>> Handle(GenerateFinalMediaReportCommand request, CancellationToken cancellationToken)
    {
        List<GacSocialPost> posts = await _queryExecutor.ToListAsync(
            _dbContext.GacSocialPosts.Where(p => p.PostedAt >= request.DateFrom && p.PostedAt <= request.DateTo), cancellationToken);
        List<GacNewsItem> news = await _queryExecutor.ToListAsync(
            _dbContext.GacNewsItems.Where(n => n.PublishedAt >= request.DateFrom && n.PublishedAt <= request.DateTo), cancellationToken);

        if (posts.Count == 0 && news.Count == 0)
        {
            NoSourceDataDto noData = await BuildNoSourceDataDiagnosticAsync(cancellationToken);
            return Result<GenerateFinalMediaReportResultDto>.Success(new GenerateFinalMediaReportResultDto(null, noData));
        }

        var feed = SourceFeedFormatter.Format(posts, news);
        FinalReportSections sections = await _sectionGenerator.GenerateAsync(
            request.PeriodLabel, request.Audience ?? "عام", request.FocusTopics, feed, cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId, "final_media_report.generate", "FinalMediaReport", request.PeriodLabel, before: null, after: null, cancellationToken);

        return Result<GenerateFinalMediaReportResultDto>.Success(new GenerateFinalMediaReportResultDto(ToDraftDto(request.PeriodLabel, sections), null));
    }

    private static FinalReportDraftDto ToDraftDto(string periodLabel, FinalReportSections sections) => new(
        periodLabel,
        sections.ExecutiveSummary,
        new ReportKpisDto(sections.Kpis.TotalNews, sections.Kpis.PositivePercent, sections.Kpis.MediaOutlets, sections.Kpis.KeyTopics, sections.Kpis.Reach, sections.Kpis.AlertsCount),
        sections.TopNews.Select(n => new TopNewsItemDto(n.Date, n.Tone, n.Headline, n.Details, n.Source)).ToList(),
        sections.Timeline.Select(t => new TimelineEventDto(t.Date, t.Event, t.Outlet, t.Tone, t.Count)).ToList(),
        new DigitalPresenceDto(
            sections.DigitalPresence.Platforms.Select(p => new DigitalPresencePlatformDto(p.Name, p.Mentions, p.Reposts, p.Engagement, p.Reach)).ToList(),
            sections.DigitalPresence.Hashtags.Select(h => new DigitalPresenceHashtagDto(h.Tag, h.Uses, h.Trend)).ToList()),
        new EditorialToneDto(
            sections.EditorialTone.Distribution.Select(b => new EditorialToneBucketDto(b.Label, b.Percent, b.Count)).ToList(),
            sections.EditorialTone.Classification.Select(b => new EditorialToneBucketDto(b.Label, b.Percent, b.Count)).ToList(),
            sections.EditorialTone.Sources.Select(b => new EditorialToneBucketDto(b.Label, b.Percent, b.Count)).ToList()),
        new DeepAnalysisDto(
            sections.DeepAnalysis.Keywords.Select(k => new DeepAnalysisKeywordDto(k.Keyword, k.Frequency, k.Context)).ToList(),
            sections.DeepAnalysis.Quote is null ? null : new DeepAnalysisQuoteDto(sections.DeepAnalysis.Quote.Text, sections.DeepAnalysis.Quote.Source, sections.DeepAnalysis.Quote.Date),
            sections.DeepAnalysis.Strengths,
            sections.DeepAnalysis.Weaknesses),
        sections.RegionalComparison.Select(r => new RegionalComparisonDto(r.Authority, r.Country, r.Mentions, r.Tone, r.Highlights)).ToList(),
        sections.Recommendations.Select(r => new RecommendationDto(r.Title, r.Description, r.Priority, r.Responsible, r.Kpi, r.Deadline, r.Dependencies)).ToList(),
        sections.Alerts.Select(a => new AlertItemDto(a.Alert, a.SuggestedPosition)).ToList(),
        sections.QuotesAppendix.Select(q => new QuoteAppendixItemDto(q.Quote, q.Source, q.Date, q.Topic)).ToList(),
        sections.Methodology,
        sections.Sources.Select(s => new SourceRefDto(s.Name, s.Url, s.Description)).ToList());

    private async Task<NoSourceDataDto> BuildNoSourceDataDiagnosticAsync(CancellationToken cancellationToken)
    {
        List<DateTimeOffset> postDates = await _queryExecutor.ToListAsync(
            _dbContext.GacSocialPosts.Where(p => p.PostedAt.HasValue).Select(p => p.PostedAt!.Value), cancellationToken);
        List<DateTimeOffset> newsDates = await _queryExecutor.ToListAsync(
            _dbContext.GacNewsItems.Where(n => n.PublishedAt.HasValue).Select(n => n.PublishedAt!.Value), cancellationToken);
        var allDates = postDates.Concat(newsDates).ToList();

        return new NoSourceDataDto(
            "NO_SOURCE_DATA",
            postDates.Count,
            newsDates.Count,
            allDates.Count == 0 ? null : allDates.Min(),
            allDates.Count == 0 ? null : allDates.Max());
    }
}
