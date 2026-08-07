using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Handles <see cref="GetShorfahIssueDocxQuery"/>. Ports <c>shorfah.ts:1094-1264</c>.</summary>
public sealed class GetShorfahIssueDocxQueryHandler : IRequestHandler<GetShorfahIssueDocxQuery, Result<byte[]>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly ShorfahExportSectionSelector _sectionSelector;
    private readonly IShorfahDocxRenderer _renderer;

    /// <summary>Initializes a new instance of the <see cref="GetShorfahIssueDocxQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="sectionSelector">The shared preview/final section selector.</param>
    /// <param name="renderer">The DOCX rendering port.</param>
    public GetShorfahIssueDocxQueryHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, ShorfahExportSectionSelector sectionSelector, IShorfahDocxRenderer renderer)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _sectionSelector = sectionSelector;
        _renderer = renderer;
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> Handle(GetShorfahIssueDocxQuery request, CancellationToken cancellationToken)
    {
        ShorfahIssue? issue = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahIssues.Where(i => i.Id == request.IssueId), cancellationToken);
        if (issue is null)
        {
            return Result<byte[]>.Failure("العدد غير موجود");
        }

        List<ShorfahSection> sections = await _sectionSelector.SelectAsync(request.IssueId, request.Preview, cancellationToken);
        var body = ShorfahIssuePlainTextBuilder.Build(issue, sections);
        var bytes = await _renderer.RenderAsync(issue.TitleAr, body, cancellationToken);
        return Result<byte[]>.Success(bytes);
    }
}
