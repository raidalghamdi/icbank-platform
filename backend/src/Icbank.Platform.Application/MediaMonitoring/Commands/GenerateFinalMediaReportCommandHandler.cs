using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring.Appearance;
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
        // The source checkboxes were previously decorative: the front end assembled a `sources` array
        // and never sent it, so unticking a channel changed nothing in the generated report.
        var selection = ReportSourceSelection.From(request.Sources);
        List<GacSocialPost> posts = await LoadPostsAsync(request, selection, cancellationToken);
        List<GacNewsItem> news = await LoadNewsAsync(request, selection, cancellationToken);

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

        MediaAppearanceAnalysisDto appearance = MediaAppearanceAnalyzer.Analyze(news, posts);
        return Result<GenerateFinalMediaReportResultDto>.Success(
            new GenerateFinalMediaReportResultDto(ToDraftDto(request.PeriodLabel, sections, appearance), null));
    }

    /// <summary>
    /// Builds the draft, replacing the two indicators the model only estimates with the counts
    /// taken from the very rows the report was generated from.
    /// </summary>
    /// <param name="periodLabel">The report's period label.</param>
    /// <param name="sections">The generated sections.</param>
    /// <param name="appearance">The measured appearance analysis for the same rows.</param>
    /// <returns>The draft read model.</returns>
    private static FinalReportDraftDto ToDraftDto(string periodLabel, FinalReportSections sections, MediaAppearanceAnalysisDto appearance) => new(
        periodLabel,
        sections.ExecutiveSummary,
        new ReportKpisDto(appearance.TotalAppearances, sections.Kpis.PositivePercent, appearance.DistinctOutlets, sections.Kpis.KeyTopics, sections.Kpis.Reach, sections.Kpis.AlertsCount),
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

    /// <summary>Loads the in-range social posts for the selected platforms.</summary>
    /// <param name="request">The generate request, carrying the date range.</param>
    /// <param name="selection">The resolved source selection.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The matching posts, or an empty list when no social channel is selected.</returns>
    private async Task<List<GacSocialPost>> LoadPostsAsync(
        GenerateFinalMediaReportCommand request, ReportSourceSelection selection, CancellationToken cancellationToken)
    {
        if (!selection.IncludeAnySocial)
        {
            return new List<GacSocialPost>();
        }

        var platforms = selection.Platforms.ToList();
        return await _queryExecutor.ToListAsync(
            _dbContext.GacSocialPosts.Where(p =>
                p.PostedAt >= request.DateFrom && p.PostedAt <= request.DateTo && platforms.Contains(p.Platform)),
            cancellationToken);
    }

    /// <summary>Loads the in-range press/news items when the news channel is selected.</summary>
    /// <param name="request">The generate request, carrying the date range.</param>
    /// <param name="selection">The resolved source selection.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The matching news items, or an empty list when news is deselected.</returns>
    private async Task<List<GacNewsItem>> LoadNewsAsync(
        GenerateFinalMediaReportCommand request, ReportSourceSelection selection, CancellationToken cancellationToken)
    {
        if (!selection.IncludeNews)
        {
            return new List<GacNewsItem>();
        }

        return await _queryExecutor.ToListAsync(
            _dbContext.GacNewsItems.Where(n => n.PublishedAt >= request.DateFrom && n.PublishedAt <= request.DateTo),
            cancellationToken);
    }

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
