using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Handles <see cref="CreateShorfahIssueCommand"/>. Ports <c>shorfah.ts:161-195</c>.
/// <c>IssueNo</c> auto-assignment (<c>max(IssueNo)+1</c> scanned in application code, not a DB
/// sequence) is preserved verbatim from the Node source, including its race-proneness under
/// concurrent creates -- flagged in WAVE4A-PORT-NOTES.md as a carried-over, not newly introduced,
/// defect (matches the identical pattern already flagged for <c>FinalReportNumberGenerator</c> in
/// WAVE3A-PORT-NOTES.md).
/// </summary>
/// <remarks>
/// Behaviour change: the Node source caught and swallowed section-seeding failures (logging only,
/// never surfacing to the caller), so an issue could be created with zero sections and no
/// indication anything went wrong. This port seeds sections in the same transaction as the issue
/// insert via <see cref="ShorfahSectionSeeder"/> -- if seeding fails, the whole creation fails
/// and nothing is persisted, which is strictly safer than a silently sectionless issue.
/// </remarks>
public sealed class CreateShorfahIssueCommandHandler : IRequestHandler<CreateShorfahIssueCommand, Result<ShorfahIssueDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;
    private readonly ShorfahSectionSeeder _sectionSeeder;

    /// <summary>Initializes a new instance of the <see cref="CreateShorfahIssueCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    /// <param name="sectionSeeder">The shared canonical-section seeder.</param>
    public CreateShorfahIssueCommandHandler(
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
    public async Task<Result<ShorfahIssueDto>> Handle(CreateShorfahIssueCommand request, CancellationToken cancellationToken)
    {
        var issueNo = request.IssueNo is > 0
            ? request.IssueNo.Value
            : await NextIssueNoAsync(cancellationToken);

        var issue = new ShorfahIssue
        {
            IssueNo = issueNo,
            TitleAr = request.TitleAr,
            SubtitleAr = request.SubtitleAr,
            Month = request.Month,
            Year = request.Year,
            ContributionsOpenAt = request.ContributionsOpenAt,
            ContributionsCloseAt = request.ContributionsCloseAt,
            EditorLetter = request.EditorLetter,
            Status = ShorfahIssueStatus.Collecting,
            CreatedByUserId = request.ActorUserId,
            CreatedAt = _dateTimeProvider.UtcNow.UtcDateTime,
            CreatedBy = ShorfahMappers.IdString(request.ActorUserId),
        };
        _dbContext.Add(issue);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var sectionsSeeded = await _sectionSeeder.SeedAsync(issue.Id, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_issue.create",
            "ShorfahIssue",
            ShorfahMappers.IdString(issue.Id),
            before: null,
            after: new { issue.IssueNo, issue.TitleAr, issue.Status, sectionsSeeded },
            cancellationToken);

        return Result<ShorfahIssueDto>.Success(ShorfahMappers.ToDto(issue));
    }

    private async Task<int> NextIssueNoAsync(CancellationToken cancellationToken)
    {
        List<int> issueNumbers = await _queryExecutor.ToListAsync(_dbContext.ShorfahIssues.Select(i => i.IssueNo), cancellationToken);
        return (issueNumbers.Count == 0 ? 0 : issueNumbers.Max()) + 1;
    }
}
