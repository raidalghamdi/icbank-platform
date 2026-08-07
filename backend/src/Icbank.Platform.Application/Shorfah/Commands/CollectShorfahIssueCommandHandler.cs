using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="CollectShorfahIssueCommand"/>. Ports <c>shorfah.ts:212-229</c>.</summary>
public sealed class CollectShorfahIssueCommandHandler : IRequestHandler<CollectShorfahIssueCommand, Result<CollectShorfahIssueResultDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;
    private readonly ShorfahSectionSeeder _sectionSeeder;

    /// <summary>Initializes a new instance of the <see cref="CollectShorfahIssueCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    /// <param name="sectionSeeder">The shared canonical-section seeder.</param>
    public CollectShorfahIssueCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IDateTimeProvider dateTimeProvider,
        IAuditLogService auditLogService,
        ShorfahSectionSeeder sectionSeeder)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
        _sectionSeeder = sectionSeeder;
    }

    /// <inheritdoc />
    public async Task<Result<CollectShorfahIssueResultDto>> Handle(CollectShorfahIssueCommand request, CancellationToken cancellationToken)
    {
        ShorfahIssue? issue = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahIssues.Where(i => i.Id == request.IssueId), cancellationToken);
        if (issue is null)
        {
            return Result<CollectShorfahIssueResultDto>.Failure("العدد غير موجود");
        }

        List<int> existingCount = await _queryExecutor.ToListAsync(
            _dbContext.ShorfahSections.Where(s => s.IssueId == request.IssueId).Select(s => s.Id), cancellationToken);

        var seeded = 0;
        if (existingCount.Count == 0)
        {
            seeded = await _sectionSeeder.SeedAsync(request.IssueId, cancellationToken);
        }

        ShorfahIssueStatus beforeStatus = issue.Status;
        if (issue.Status != ShorfahIssueStatus.Published)
        {
            issue.Status = ShorfahIssueStatus.Collecting;
        }

        issue.UpdatedAt = _dateTimeProvider.UtcNow.UtcDateTime;
        issue.UpdatedBy = ShorfahMappers.IdString(request.ActorUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_issue.collect",
            "ShorfahIssue",
            ShorfahMappers.IdString(issue.Id),
            before: new { Status = beforeStatus },
            after: new { issue.Status, sectionsSeeded = seeded, sectionsExisting = existingCount.Count },
            cancellationToken);

        return Result<CollectShorfahIssueResultDto>.Success(
            new CollectShorfahIssueResultDto(ShorfahMappers.ToDto(issue), seeded, existingCount.Count));
    }
}
