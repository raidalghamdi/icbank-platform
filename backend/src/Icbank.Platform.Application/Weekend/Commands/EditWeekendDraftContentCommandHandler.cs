using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Handles <see cref="EditWeekendDraftContentCommand"/>.</summary>
public sealed class EditWeekendDraftContentCommandHandler : IRequestHandler<EditWeekendDraftContentCommand, Result<WeekendDraftDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="EditWeekendDraftContentCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public EditWeekendDraftContentCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<WeekendDraftDto>> Handle(EditWeekendDraftContentCommand request, CancellationToken cancellationToken)
    {
        WeekendDraft? draft = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.WeekendDrafts.Where(d => d.Id == request.DraftId), cancellationToken);
        if (draft is null)
        {
            return Result<WeekendDraftDto>.Failure("المسودة غير موجودة");
        }

        draft.ContentJson = request.ContentJson;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "weekend_draft.edit_content",
            "WeekendDraft",
            draft.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: null,
            cancellationToken);

        return Result<WeekendDraftDto>.Success(ToDto(draft));
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
