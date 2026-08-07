using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Handles <see cref="DeleteWeekendDraftCommand"/>.</summary>
public sealed class DeleteWeekendDraftCommandHandler : IRequestHandler<DeleteWeekendDraftCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="DeleteWeekendDraftCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public DeleteWeekendDraftCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteWeekendDraftCommand request, CancellationToken cancellationToken)
    {
        WeekendDraft? draft = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.WeekendDrafts.Where(d => d.Id == request.DraftId), cancellationToken);
        if (draft is null)
        {
            return Result<bool>.Failure("المسودة غير موجودة");
        }

        _dbContext.Remove(draft);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "weekend_draft.delete",
            "WeekendDraft",
            request.DraftId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: new { draft.WeekendDate },
            after: null,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
