using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Handles <see cref="GetWeekendDraftByIdQuery"/>.</summary>
public sealed class GetWeekendDraftByIdQueryHandler : IRequestHandler<GetWeekendDraftByIdQuery, Result<WeekendDraftDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetWeekendDraftByIdQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetWeekendDraftByIdQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<WeekendDraftDto>> Handle(GetWeekendDraftByIdQuery request, CancellationToken cancellationToken)
    {
        WeekendDraft? draft = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.WeekendDrafts.Where(d => d.Id == request.DraftId), cancellationToken);

        return draft is null
            ? Result<WeekendDraftDto>.Failure("المسودة غير موجودة")
            : Result<WeekendDraftDto>.Success(ToDto(draft));
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
