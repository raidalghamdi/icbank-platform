using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.InternationalDays;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Commands;

/// <summary>
/// Handles <see cref="SaveInternationalDayCommand"/>. Closes DEFECT-LOG.md DATA-05: the Node
/// source performed this multi-step upsert across 4 tables with no transaction; this handler
/// batches every entity change into a single <see cref="IApplicationDbContext.SaveChangesAsync"/>
/// call, which EF Core wraps in one implicit database transaction, so a mid-sequence failure
/// leaves no partial rows committed.
/// </summary>
public sealed class SaveInternationalDayCommandHandler : IRequestHandler<SaveInternationalDayCommand, Result<SaveInternationalDayResultDto>>
{
    private const string DesignSampleActivationType = "تصميم بصري";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="SaveInternationalDayCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public SaveInternationalDayCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider dateTimeProvider, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<SaveInternationalDayResultDto>> Handle(SaveInternationalDayCommand request, CancellationToken cancellationToken)
    {
        DaySearchResultDto data = request.Data;
        DateTimeOffset now = _dateTimeProvider.UtcNow;
        var currentYear = now.Year;

        InternationalDay day = await UpsertDayAsync(data, request.Category, now, cancellationToken);
        await UpsertThemeAsync(day, data, currentYear, cancellationToken);
        await SaveActivationsAsync(day.Id, data, currentYear, cancellationToken);
        await SaveDesignSamplesAsync(day.Id, data, currentYear, cancellationToken);
        SaveSources(day.Id, data);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "international_day.save",
            "InternationalDay",
            day.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { day.DayNameAr },
            cancellationToken);

        return Result<SaveInternationalDayResultDto>.Success(new SaveInternationalDayResultDto(day.Id, ToDto(day)));
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

    private async Task<InternationalDay> UpsertDayAsync(DaySearchResultDto data, string? category, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var dayNameAr = data.DayNameAr!;
        InternationalDay? existing = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.InternationalDays.Where(d => d.DayNameAr.Contains(dayNameAr)).Take(1), cancellationToken);

        if (existing is not null)
        {
            existing.DayNameEn = data.DayNameEn;
            existing.AnnualDate = data.AnnualDate;
            existing.Category = category ?? existing.Category;
            existing.OfficialOrganizer = data.OfficialOrganizer;
            existing.OfficialOrganizerSource = data.OfficialOrganizerSource;
            existing.HistorySummary = data.HistorySummary;
            existing.HistorySource = data.HistorySource;
            existing.Suggestions = data.Suggestions?.ToList() ?? existing.Suggestions;
            existing.LastSearchedAt = now;
            return existing;
        }

        var inserted = new InternationalDay
        {
            DayNameAr = dayNameAr,
            DayNameEn = data.DayNameEn,
            AnnualDate = data.AnnualDate,
            Category = category,
            OfficialOrganizer = data.OfficialOrganizer,
            OfficialOrganizerSource = data.OfficialOrganizerSource,
            HistorySummary = data.HistorySummary,
            HistorySource = data.HistorySource,
            Suggestions = data.Suggestions?.ToList() ?? new List<string>(),
            LastSearchedAt = now,
        };
        _dbContext.Add(inserted);
        return inserted;
    }

    private async Task UpsertThemeAsync(InternationalDay day, DaySearchResultDto data, int currentYear, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(data.CurrentThemeAr) && string.IsNullOrEmpty(data.CurrentThemeEn))
        {
            return;
        }

        DayYearlyTheme? themeExists = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.DayYearlyThemes.Where(t => t.DayId == day.Id && t.Year == currentYear), cancellationToken);

        if (themeExists is not null)
        {
            themeExists.ThemeAr = data.CurrentThemeAr;
            themeExists.ThemeEn = data.CurrentThemeEn;
            themeExists.ThemeSourceUrl = data.ThemeSourceUrl;
            return;
        }

        _dbContext.Add(new DayYearlyTheme
        {
            Day = day,
            Year = currentYear,
            ThemeAr = data.CurrentThemeAr,
            ThemeEn = data.CurrentThemeEn,
            ThemeSourceUrl = data.ThemeSourceUrl,
        });
    }

    private async Task SaveActivationsAsync(int dayId, DaySearchResultDto data, int currentYear, CancellationToken cancellationToken)
    {
        foreach (DaySearchActivationDto activation in data.Activations ?? Array.Empty<DaySearchActivationDto>())
        {
            var entityName = activation.EntityName ?? string.Empty;
            var year = activation.Year ?? currentYear;
            var exists = await _queryExecutor.AnyAsync(
                _dbContext.DayActivations.Where(a => a.DayId == dayId && a.EntityName == entityName && a.Year == year), cancellationToken);
            if (exists)
            {
                continue;
            }

            _dbContext.Add(new DayActivation
            {
                DayId = dayId,
                Year = year,
                EntityName = activation.EntityName,
                EntityType = activation.EntityType,
                ActivationType = activation.ActivationType,
                Platform = activation.Platform,
                Description = activation.Description,
                SourceUrl = activation.SourceUrl,
                Country = activation.Country,
                Verified = !string.IsNullOrEmpty(activation.SourceUrl),
            });
        }
    }

    private async Task SaveDesignSamplesAsync(int dayId, DaySearchResultDto data, int currentYear, CancellationToken cancellationToken)
    {
        foreach (DaySearchDesignSampleDto sample in data.DesignSamples ?? Array.Empty<DaySearchDesignSampleDto>())
        {
            if (string.IsNullOrEmpty(sample.EntityName))
            {
                continue;
            }

            var year = sample.Year ?? currentYear;
            var exists = await _queryExecutor.AnyAsync(
                _dbContext.DayActivations.Where(a =>
                    a.DayId == dayId && a.EntityName == sample.EntityName && a.ActivationType == DesignSampleActivationType && a.Year == year),
                cancellationToken);
            if (exists)
            {
                continue;
            }

            var description = string.Join(" ", new[] { sample.Platform is null ? null : $"[{sample.Platform}]", sample.Description }
                .Where(part => !string.IsNullOrEmpty(part)));

            _dbContext.Add(new DayActivation
            {
                DayId = dayId,
                Year = year,
                EntityName = sample.EntityName,
                EntityType = sample.EntityType,
                ActivationType = DesignSampleActivationType,
                Description = description,
                SourceUrl = sample.PageUrl ?? sample.ImageUrl,
                Country = sample.Country,
                Verified = !string.IsNullOrEmpty(sample.PageUrl) || !string.IsNullOrEmpty(sample.ImageUrl),
            });
        }
    }

    private void SaveSources(int dayId, DaySearchResultDto data)
    {
        foreach (DaySearchSourceDto source in data.Sources ?? Array.Empty<DaySearchSourceDto>())
        {
            if (string.IsNullOrEmpty(source.Url))
            {
                continue;
            }

            _dbContext.Add(new IntlDaySource
            {
                RelatedTable = "international_days",
                RelatedId = dayId,
                DayId = dayId,
                SourceUrl = source.Url,
                SourceTitle = source.Title,
                SourcePublisher = source.Publisher,
                AccessedAt = _dateTimeProvider.UtcNow,
            });
        }
    }
}
