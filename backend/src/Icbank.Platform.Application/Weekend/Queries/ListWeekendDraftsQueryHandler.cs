using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Handles <see cref="ListWeekendDraftsQuery"/>.</summary>
public sealed class ListWeekendDraftsQueryHandler : IRequestHandler<ListWeekendDraftsQuery, Result<PagedResult<WeekendDraftDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListWeekendDraftsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListWeekendDraftsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<WeekendDraftDto>>> Handle(ListWeekendDraftsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<WeekendDraft> filtered = _dbContext.WeekendDrafts;
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<WeekendDraftStatus>(request.Status, ignoreCase: true, out WeekendDraftStatus status))
        {
            filtered = filtered.Where(d => d.Status == status);
        }

        IQueryable<WeekendDraft> ordered = filtered.OrderByDescending(d => d.CreatedAt);
        var total = (await _queryExecutor.ToListAsync(ordered, cancellationToken)).Count;

        List<WeekendDraft> page = await _queryExecutor.ToListAsync(
            ordered.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);

        var items = page.Select(ToDto).ToList();
        return Result<PagedResult<WeekendDraftDto>>.Success(new PagedResult<WeekendDraftDto>(items, request.Query.Page, request.Query.PageSize, total));
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
