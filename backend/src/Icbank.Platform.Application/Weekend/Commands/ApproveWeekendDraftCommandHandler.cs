using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Handles <see cref="ApproveWeekendDraftCommand"/>. Ports BUSINESS-RULES.md §2.2's hard precondition verbatim.</summary>
public sealed class ApproveWeekendDraftCommandHandler : IRequestHandler<ApproveWeekendDraftCommand, Result<WeekendDraftDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="ApproveWeekendDraftCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public ApproveWeekendDraftCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider dateTimeProvider, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<WeekendDraftDto>> Handle(ApproveWeekendDraftCommand request, CancellationToken cancellationToken)
    {
        WeekendDraft? draft = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.WeekendDrafts.Where(d => d.Id == request.DraftId), cancellationToken);
        if (draft is null)
        {
            return Result<WeekendDraftDto>.Failure("المسودة غير موجودة");
        }

        if (draft.Status != WeekendDraftStatus.PendingReview)
        {
            return Result<WeekendDraftDto>.Failure($"لا يمكن اعتماد مسودة بحالة {draft.Status}");
        }

        draft.Status = WeekendDraftStatus.Approved;
        draft.ApprovedByUserId = request.ActorUserId;
        draft.ApprovedAt = _dateTimeProvider.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "weekend_draft.approve",
            "WeekendDraft",
            draft.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { draft.Status },
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
