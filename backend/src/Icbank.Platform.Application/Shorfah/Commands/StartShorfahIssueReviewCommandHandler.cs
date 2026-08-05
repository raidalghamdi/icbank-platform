using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="StartShorfahIssueReviewCommand"/>. Ports <c>shorfah.ts:248-258</c>.</summary>
public sealed class StartShorfahIssueReviewCommandHandler : IRequestHandler<StartShorfahIssueReviewCommand, Result<ShorfahIssueDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="StartShorfahIssueReviewCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public StartShorfahIssueReviewCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider dateTimeProvider, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<ShorfahIssueDto>> Handle(StartShorfahIssueReviewCommand request, CancellationToken cancellationToken)
    {
        ShorfahIssue? issue = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahIssues.Where(i => i.Id == request.IssueId), cancellationToken);
        if (issue is null)
        {
            return Result<ShorfahIssueDto>.Failure("العدد غير موجود");
        }

        if (!ShorfahIssueStateMachine.CanStartReview(issue.Status))
        {
            return Result<ShorfahIssueDto>.Failure("العدد منشور بالفعل");
        }

        ShorfahIssueStatus beforeStatus = issue.Status;
        issue.Status = ShorfahIssueStatus.InReview;
        issue.UpdatedAt = _dateTimeProvider.UtcNow.UtcDateTime;
        issue.UpdatedBy = ShorfahMappers.IdString(request.ActorUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_issue.start_review",
            "ShorfahIssue",
            ShorfahMappers.IdString(issue.Id),
            before: new { Status = beforeStatus },
            after: new { issue.Status },
            cancellationToken);

        return Result<ShorfahIssueDto>.Success(ShorfahMappers.ToDto(issue));
    }
}
