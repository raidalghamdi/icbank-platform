using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.InternationalDays;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Queries;

/// <summary>
/// Handles <see cref="ListInternationalDaysArchiveQuery"/>. Closes DEFECT-LOG.md DATA-06: the
/// Node source ran one extra query pair per day row; this handler instead issues one batched
/// query for the current page's themes and one for activation counts, using <c>IN</c>-style
/// filters instead of a query-per-row loop.
/// </summary>
public sealed class ListInternationalDaysArchiveQueryHandler
    : IRequestHandler<ListInternationalDaysArchiveQuery, Result<PagedResult<InternationalDayArchiveItemDto>>>
{
    private const int ThemesPerDay = 3;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListInternationalDaysArchiveQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListInternationalDaysArchiveQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<InternationalDayArchiveItemDto>>> Handle(
        ListInternationalDaysArchiveQuery request, CancellationToken cancellationToken)
    {
        IOrderedQueryable<InternationalDay> query = ApplyFilters(_dbContext.InternationalDays, request).OrderByDescending(d => d.AnnualDate).ThenByDescending(d => d.UpdatedAt);

        List<int> allIds = await _queryExecutor.ToListAsync(query.Select(d => d.Id), cancellationToken);
        var total = allIds.Count;
        List<InternationalDay> pageDays = await _queryExecutor.ToListAsync(
            query.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);

        List<InternationalDayArchiveItemDto> items = await BuildItemsAsync(pageDays, request.Year, cancellationToken);

        return Result<PagedResult<InternationalDayArchiveItemDto>>.Success(
            new PagedResult<InternationalDayArchiveItemDto>(items, request.Query.Page, request.Query.PageSize, total));
    }

    private static IQueryable<InternationalDay> ApplyFilters(IQueryable<InternationalDay> query, ListInternationalDaysArchiveQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var pattern = request.SearchText.Trim();
            query = query.Where(d => d.DayNameAr.Contains(pattern) || (d.DayNameEn != null && d.DayNameEn.Contains(pattern)));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(d => d.Category == request.Category);
        }

        return query;
    }

    private static InternationalDayDto ToDto(InternationalDay day) => new(
        day.Id,
        day.DayNameAr,
        day.DayNameEn,
        day.AnnualDate,
        day.Category,
        day.OfficialOrganizer,
        day.OfficialOrganizerSource,
        day.HistorySummary,
        day.HistorySource,
        day.Suggestions,
        day.LastSearchedAt);

    private async Task<List<InternationalDayArchiveItemDto>> BuildItemsAsync(List<InternationalDay> pageDays, int? year, CancellationToken cancellationToken)
    {
        var pageDayIds = pageDays.Select(d => d.Id).ToList();

        IQueryable<DayYearlyTheme> themesQuery = _dbContext.DayYearlyThemes.Where(t => pageDayIds.Contains(t.DayId));
        if (year.HasValue)
        {
            themesQuery = themesQuery.Where(t => t.Year == year.Value);
        }

        List<DayYearlyTheme> allThemes = await _queryExecutor.ToListAsync(themesQuery.OrderByDescending(t => t.Year), cancellationToken);
        List<int> activationCounts = await _queryExecutor.ToListAsync(
            _dbContext.DayActivations.Where(a => pageDayIds.Contains(a.DayId)).Select(a => a.DayId), cancellationToken);

        return pageDays.Select(day =>
        {
            var themes = allThemes.Where(t => t.DayId == day.Id).Take(ThemesPerDay)
                .Select(t => new DayYearlyThemeDto(t.Id, t.Year, t.ThemeAr, t.ThemeEn, t.ThemeSourceUrl)).ToList();
            var activationCount = activationCounts.Count(id => id == day.Id);
            return new InternationalDayArchiveItemDto(ToDto(day), themes, activationCount);
        }).ToList();
    }
}
