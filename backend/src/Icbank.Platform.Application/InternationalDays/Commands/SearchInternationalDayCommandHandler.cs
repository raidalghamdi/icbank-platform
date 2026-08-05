using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.InternationalDays;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Commands;

/// <summary>
/// Handles <see cref="SearchInternationalDayCommand"/>. Ports BUSINESS-RULES.md §4.1's per-IP
/// rate limit and §4.5's 7-day fuzzy-match cache. The cache-hit fuzzy-substring-match precision
/// gap flagged in BUSINESS-RULES.md §4.5 (searching a common word can match unrelated records) is
/// preserved as-is, matching the Node source's <c>ILIKE '%query%'</c> semantics via <c>Contains</c>.
/// </summary>
public sealed class SearchInternationalDayCommandHandler : IRequestHandler<SearchInternationalDayCommand, Result<SearchInternationalDayResultDto>>
{
    private static readonly TimeSpan CacheWindow = TimeSpan.FromDays(7);

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IInternationalDaySearchProvider _searchProvider;
    private readonly IInternationalDaySearchRateLimiter _rateLimiter;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new instance of the <see cref="SearchInternationalDayCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="searchProvider">The AI-backed (or placeholder) search port.</param>
    /// <param name="rateLimiter">The per-IP search rate limiter port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    public SearchInternationalDayCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IInternationalDaySearchProvider searchProvider,
        IInternationalDaySearchRateLimiter rateLimiter,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _searchProvider = searchProvider;
        _rateLimiter = rateLimiter;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<SearchInternationalDayResultDto>> Handle(SearchInternationalDayCommand request, CancellationToken cancellationToken)
    {
        if (!_rateLimiter.TryConsume(request.IpAddress))
        {
            return Result<SearchInternationalDayResultDto>.Failure("تجاوزت حد 10 عمليات بحث في الساعة. حاول لاحقاً.");
        }

        var trimmedQuery = request.Query.Trim();
        DateTimeOffset now = _dateTimeProvider.UtcNow;

        if (!request.ForceRefresh)
        {
            CachedDayResult? cachedResult = await TryServeFromCacheAsync(trimmedQuery, now, cancellationToken);
            if (cachedResult is not null)
            {
                await LogSearchAsync(trimmedQuery, request.IpAddress, cachedResult.DayId, cancellationToken);
                return Result<SearchInternationalDayResultDto>.Success(
                    new SearchInternationalDayResultDto(true, _rateLimiter.GetRemaining(request.IpAddress), request.Category, cachedResult.Data));
            }
        }

        await LogSearchAsync(trimmedQuery, request.IpAddress, dayId: null, cancellationToken);

        var currentYear = now.Year;
        DaySearchResultDto searchResult = await _searchProvider.SearchAsync(trimmedQuery, currentYear, cancellationToken);

        return Result<SearchInternationalDayResultDto>.Success(
            new SearchInternationalDayResultDto(false, _rateLimiter.GetRemaining(request.IpAddress), request.Category, searchResult));
    }

    private async Task<CachedDayResult?> TryServeFromCacheAsync(string query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = now - CacheWindow;
        InternationalDay? existing = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.InternationalDays
                .Where(d => d.DayNameAr.Contains(query) && d.LastSearchedAt != null && d.LastSearchedAt > cutoff)
                .OrderByDescending(d => d.LastSearchedAt)
                .Take(1),
            cancellationToken);

        if (existing is null)
        {
            return null;
        }

        DaySearchResultDto data = await BuildCachedDataAsync(existing, cancellationToken);
        return new CachedDayResult(existing.Id, data);
    }

    private async Task<DaySearchResultDto> BuildCachedDataAsync(InternationalDay existing, CancellationToken cancellationToken)
    {
        List<DayYearlyTheme> themes = await _queryExecutor.ToListAsync(
            _dbContext.DayYearlyThemes.Where(t => t.DayId == existing.Id).OrderByDescending(t => t.Year), cancellationToken);
        List<DayActivation> activations = await _queryExecutor.ToListAsync(
            _dbContext.DayActivations.Where(a => a.DayId == existing.Id).OrderByDescending(a => a.Year), cancellationToken);
        List<IntlDaySource> sources = await _queryExecutor.ToListAsync(
            _dbContext.IntlDaySources.Where(s => s.RelatedTable == "international_days" && s.RelatedId == existing.Id), cancellationToken);

        DayYearlyTheme? latestTheme = themes.FirstOrDefault();
        return new DaySearchResultDto(
            existing.DayNameAr,
            existing.DayNameEn,
            existing.AnnualDate,
            existing.OfficialOrganizer,
            existing.OfficialOrganizerSource,
            existing.HistorySummary,
            existing.HistorySource,
            latestTheme?.ThemeAr,
            latestTheme?.ThemeEn,
            latestTheme?.ThemeSourceUrl,
            activations.Select(a => new DaySearchActivationDto(a.EntityName, a.EntityType, a.ActivationType, a.Platform, a.Description, a.SourceUrl, a.Country, a.Year)).ToList(),
            Array.Empty<DaySearchDesignSampleDto>(),
            existing.Suggestions,
            sources.Select(s => new DaySearchSourceDto(s.SourceUrl, s.SourceTitle, s.SourcePublisher)).ToList());
    }

    private async Task LogSearchAsync(string query, string ipAddress, int? dayId, CancellationToken cancellationToken)
    {
        _dbContext.Add(new IntlSearchHistory
        {
            Query = query,
            DayId = dayId,
            IpAddress = ipAddress,
            SearchedAt = _dateTimeProvider.UtcNow,
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record CachedDayResult(int DayId, DaySearchResultDto Data);
}
