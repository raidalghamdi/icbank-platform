using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="SeedShorfahIssueSectionsCommand"/>. Ports <c>shorfah.ts:199-209</c>.</summary>
public sealed class SeedShorfahIssueSectionsCommandHandler : IRequestHandler<SeedShorfahIssueSectionsCommand, Result<int>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;
    private readonly ShorfahSectionSeeder _sectionSeeder;

    /// <summary>Initializes a new instance of the <see cref="SeedShorfahIssueSectionsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    /// <param name="sectionSeeder">The shared canonical-section seeder.</param>
    public SeedShorfahIssueSectionsCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService, ShorfahSectionSeeder sectionSeeder)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
        _sectionSeeder = sectionSeeder;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(SeedShorfahIssueSectionsCommand request, CancellationToken cancellationToken)
    {
        var issueExists = await _queryExecutor.AnyAsync(_dbContext.ShorfahIssues.Where(i => i.Id == request.IssueId), cancellationToken);
        if (!issueExists)
        {
            return Result<int>.Failure("العدد غير موجود");
        }

        List<int> existing = await _queryExecutor.ToListAsync(
            _dbContext.ShorfahSections.Where(s => s.IssueId == request.IssueId).Select(s => s.Id), cancellationToken);
        if (existing.Count > 0)
        {
            return Result<int>.Failure("هذا العدد يحتوي على أقسام بالفعل");
        }

        var seeded = await _sectionSeeder.SeedAsync(request.IssueId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_issue.seed_sections",
            "ShorfahIssue",
            ShorfahMappers.IdString(request.IssueId),
            before: null,
            after: new { sectionsSeeded = seeded },
            cancellationToken);

        return Result<int>.Success(seeded);
    }
}
