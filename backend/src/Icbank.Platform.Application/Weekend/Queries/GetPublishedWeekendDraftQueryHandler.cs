using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Handles <see cref="GetPublishedWeekendDraftQuery"/>.</summary>
public sealed class GetPublishedWeekendDraftQueryHandler : IRequestHandler<GetPublishedWeekendDraftQuery, Result<WeekendDraftDto?>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new instance of the <see cref="GetPublishedWeekendDraftQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock, resolved to Asia/Riyadh for the default target date.</param>
    public GetPublishedWeekendDraftQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<WeekendDraftDto?>> Handle(GetPublishedWeekendDraftQuery request, CancellationToken cancellationToken)
    {
        var targetDate = string.IsNullOrWhiteSpace(request.TargetDate)
            ? WeekendCadenceCalculator.NextThursday(_dateTimeProvider.RiyadhNow)
            : request.TargetDate;

        List<WeekendDraft> published = await _queryExecutor.ToListAsync(
            _dbContext.WeekendDrafts.Where(d => d.Status == WeekendDraftStatus.Published), cancellationToken);

        WeekendDraft? exactMatch = published
            .Where(d => d.WeekendDate == targetDate)
            .OrderByDescending(d => d.PublishedAt)
            .FirstOrDefault();

        WeekendDraft? resolved = exactMatch ?? published.OrderByDescending(d => d.PublishedAt).FirstOrDefault();

        return Result<WeekendDraftDto?>.Success(resolved is null ? null : ToDto(resolved));
    }

    private static WeekendDraftDto ToDto(WeekendDraft draft) => new(
        draft.Id,
        draft.WeekendDate,
        draft.City,
        draft.Status.ToString(),
        draft.ModelName,
        draft.ContentJson,
        draft.GeneratedByUserId,
        draft.ApprovedByUserId,
        draft.RejectedReason,
        draft.ApprovedAt,
        draft.PublishedAt);
}
