using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>
/// Handles <see cref="GetShorfahIssueByIdQuery"/>. Ports <c>shorfah.ts:92-124</c>. This port does
/// not attach media/permissions per-section the way the Node source's enriched response did
/// (BUSINESS-RULES.md flags the Node approach as N+1-ish: it pulls all media/permissions for
/// every section then filters in JS) -- media and permissions belong to wave 4b's scope per the
/// task's explicit boundary, so <see cref="ShorfahSectionDto"/> intentionally omits them here.
/// </summary>
public sealed class GetShorfahIssueByIdQueryHandler : IRequestHandler<GetShorfahIssueByIdQuery, Result<ShorfahIssueDetailDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetShorfahIssueByIdQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetShorfahIssueByIdQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<ShorfahIssueDetailDto>> Handle(GetShorfahIssueByIdQuery request, CancellationToken cancellationToken)
    {
        ShorfahIssue? issue = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahIssues.Where(i => i.Id == request.IssueId), cancellationToken);
        if (issue is null)
        {
            return Result<ShorfahIssueDetailDto>.Failure("العدد غير موجود");
        }

        List<ShorfahSection> sections = await _queryExecutor.ToListAsync(
            _dbContext.ShorfahSections.Where(s => s.IssueId == request.IssueId).OrderBy(s => s.DisplayOrder), cancellationToken);

        var dto = new ShorfahIssueDetailDto(ShorfahMappers.ToDto(issue), sections.Select(ShorfahMappers.ToDto).ToList());
        return Result<ShorfahIssueDetailDto>.Success(dto);
    }
}
