using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.InternationalDays;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Queries;

/// <summary>Handles <see cref="GetInternationalDayByIdQuery"/>.</summary>
public sealed class GetInternationalDayByIdQueryHandler : IRequestHandler<GetInternationalDayByIdQuery, Result<InternationalDayDetailDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetInternationalDayByIdQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetInternationalDayByIdQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<InternationalDayDetailDto>> Handle(GetInternationalDayByIdQuery request, CancellationToken cancellationToken)
    {
        InternationalDay? day = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.InternationalDays.Where(d => d.Id == request.DayId), cancellationToken);
        if (day is null)
        {
            return Result<InternationalDayDetailDto>.Failure("غير موجود");
        }

        List<DayYearlyTheme> themes = await _queryExecutor.ToListAsync(
            _dbContext.DayYearlyThemes.Where(t => t.DayId == day.Id).OrderByDescending(t => t.Year), cancellationToken);
        List<DayActivation> activations = await _queryExecutor.ToListAsync(
            _dbContext.DayActivations.Where(a => a.DayId == day.Id).OrderByDescending(a => a.Year), cancellationToken);
        List<IntlDaySource> sources = await _queryExecutor.ToListAsync(
            _dbContext.IntlDaySources.Where(s => s.RelatedTable == "international_days" && s.RelatedId == day.Id), cancellationToken);

        var detail = new InternationalDayDetailDto(
            ToDto(day),
            themes.Select(t => new DayYearlyThemeDto(t.Id, t.Year, t.ThemeAr, t.ThemeEn, t.ThemeSourceUrl)).ToList(),
            activations.Select(a => new DayActivationDto(a.Id, a.Year, a.EntityName, a.EntityType, a.ActivationType, a.Platform, a.Description, a.SourceUrl, a.Country, a.Verified)).ToList(),
            sources.Select(s => new IntlDaySourceDto(s.Id, s.SourceUrl, s.SourceTitle, s.SourcePublisher)).ToList());

        return Result<InternationalDayDetailDto>.Success(detail);
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
}
