using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="UpdateShorfahIssueCommand"/>. Ports <c>shorfah.ts:231-244</c>.</summary>
public sealed class UpdateShorfahIssueCommandHandler : IRequestHandler<UpdateShorfahIssueCommand, Result<ShorfahIssueDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="UpdateShorfahIssueCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public UpdateShorfahIssueCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider dateTimeProvider, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<ShorfahIssueDto>> Handle(UpdateShorfahIssueCommand request, CancellationToken cancellationToken)
    {
        ShorfahIssue? issue = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahIssues.Where(i => i.Id == request.IssueId), cancellationToken);
        if (issue is null)
        {
            return Result<ShorfahIssueDto>.Failure("العدد غير موجود");
        }

        if (request.Status is not null && !TryApplyStatus(issue, request.Status))
        {
            return Result<ShorfahIssueDto>.Failure($"لا يمكن تغيير حالة العدد من {issue.Status} إلى {request.Status}");
        }

        var before = new { issue.TitleAr, issue.SubtitleAr, issue.EditorLetter, issue.CoverImageUrl, issue.Status };

        issue.TitleAr = request.TitleAr ?? issue.TitleAr;
        issue.SubtitleAr = request.SubtitleAr ?? issue.SubtitleAr;
        issue.EditorLetter = request.EditorLetter ?? issue.EditorLetter;
        issue.CoverImageUrl = request.CoverImageUrl ?? issue.CoverImageUrl;
        issue.ContributionsOpenAt = request.ContributionsOpenAt ?? issue.ContributionsOpenAt;
        issue.ContributionsCloseAt = request.ContributionsCloseAt ?? issue.ContributionsCloseAt;
        issue.UpdatedAt = _dateTimeProvider.UtcNow.UtcDateTime;
        issue.UpdatedBy = ShorfahMappers.IdString(request.ActorUserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_issue.update",
            "ShorfahIssue",
            ShorfahMappers.IdString(issue.Id),
            before,
            after: new { issue.TitleAr, issue.SubtitleAr, issue.EditorLetter, issue.CoverImageUrl, issue.Status },
            cancellationToken);

        return Result<ShorfahIssueDto>.Success(ShorfahMappers.ToDto(issue));
    }

    private static bool TryApplyStatus(ShorfahIssue issue, string requestedStatus)
    {
        if (!Enum.TryParse<ShorfahIssueStatus>(requestedStatus, ignoreCase: true, out ShorfahIssueStatus target))
        {
            return false;
        }

        if (!ShorfahIssueStateMachine.CanTransitionTo(issue.Status, target))
        {
            return false;
        }

        issue.Status = target;
        return true;
    }
}
