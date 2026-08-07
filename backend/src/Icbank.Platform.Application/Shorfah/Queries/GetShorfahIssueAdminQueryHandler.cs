using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Handles <see cref="GetShorfahIssueAdminQuery"/>. Ports <c>shorfah.ts:829-851</c>.</summary>
public sealed class GetShorfahIssueAdminQueryHandler : IRequestHandler<GetShorfahIssueAdminQuery, Result<ShorfahIssueAdminDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetShorfahIssueAdminQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetShorfahIssueAdminQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<ShorfahIssueAdminDto>> Handle(GetShorfahIssueAdminQuery request, CancellationToken cancellationToken)
    {
        var issueExists = await _queryExecutor.AnyAsync(_dbContext.ShorfahIssues.Where(i => i.Id == request.IssueId), cancellationToken);
        if (!issueExists)
        {
            return Result<ShorfahIssueAdminDto>.Failure("العدد غير موجود");
        }

        List<ShorfahSection> sections = await _queryExecutor.ToListAsync(
            _dbContext.ShorfahSections.Where(s => s.IssueId == request.IssueId).OrderBy(s => s.DisplayOrder), cancellationToken);
        var sectionIds = sections.Select(s => s.Id).ToList();

        List<ShorfahAssignment> assignments = await _queryExecutor.ToListAsync(
            _dbContext.ShorfahAssignments.Where(a => sectionIds.Contains(a.SectionId)), cancellationToken);

        List<ShorfahReminder> reminders = await _queryExecutor.ToListAsync(
            _dbContext.ShorfahReminders.Where(r => sectionIds.Contains(r.SectionId)).OrderByDescending(r => r.SentAt), cancellationToken);

        var dto = new ShorfahIssueAdminDto(
            sections.Select(ShorfahMappers.ToDto).ToList(),
            assignments.Select(ShorfahMappers.ToDto).ToList(),
            reminders.Select(ShorfahMappers.ToDto).ToList());
        return Result<ShorfahIssueAdminDto>.Success(dto);
    }
}
