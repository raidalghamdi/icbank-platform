using System.Globalization;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.InternationalDays;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Queries;

/// <summary>Handles <see cref="ExportInternationalDayHtmlQuery"/>. See <see cref="InternationalDayHtmlExportBuilder"/> for the H-1/SEC-21 encoding fix.</summary>
public sealed class ExportInternationalDayHtmlQueryHandler : IRequestHandler<ExportInternationalDayHtmlQuery, Result<InternationalDayHtmlExportDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new instance of the <see cref="ExportInternationalDayHtmlQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock, resolved to Asia/Riyadh for the export timestamp.</param>
    public ExportInternationalDayHtmlQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<InternationalDayHtmlExportDto>> Handle(ExportInternationalDayHtmlQuery request, CancellationToken cancellationToken)
    {
        InternationalDay? day = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.InternationalDays.Where(d => d.Id == request.DayId), cancellationToken);
        if (day is null)
        {
            return Result<InternationalDayHtmlExportDto>.Failure("غير موجود");
        }

        List<DayYearlyTheme> themes = await _queryExecutor.ToListAsync(
            _dbContext.DayYearlyThemes.Where(t => t.DayId == day.Id).OrderByDescending(t => t.Year), cancellationToken);
        List<DayActivation> activations = await _queryExecutor.ToListAsync(
            _dbContext.DayActivations.Where(a => a.DayId == day.Id).OrderByDescending(a => a.Year), cancellationToken);
        List<IntlDaySource> sources = await _queryExecutor.ToListAsync(
            _dbContext.IntlDaySources.Where(s => s.RelatedTable == "international_days" && s.RelatedId == day.Id), cancellationToken);

        DayYearlyTheme? latestTheme = themes.FirstOrDefault();
        DateTimeOffset riyadhNow = _dateTimeProvider.RiyadhNow;

        var model = new InternationalDayExportModel(
            day.DayNameAr,
            day.DayNameEn,
            day.AnnualDate,
            day.OfficialOrganizer,
            day.Category,
            day.HistorySummary,
            day.HistorySource,
            riyadhNow.Year.ToString(CultureInfo.InvariantCulture),
            latestTheme?.ThemeAr,
            latestTheme?.ThemeEn,
            latestTheme?.ThemeSourceUrl,
            activations.Select(a => new InternationalDayExportActivation(a.EntityName, a.EntityType, a.ActivationType, a.Description, a.Country, a.Year, a.SourceUrl, a.Verified)).ToList(),
            day.Suggestions,
            sources.Select(s => new InternationalDayExportSource(s.SourceUrl, s.SourceTitle, s.SourcePublisher)).ToList(),
            riyadhNow.ToString("d MMMM yyyy", CultureInfo.InvariantCulture));

        var html = InternationalDayHtmlExportBuilder.Build(model);
        return Result<InternationalDayHtmlExportDto>.Success(new InternationalDayHtmlExportDto(day.DayNameAr, html));
    }
}
