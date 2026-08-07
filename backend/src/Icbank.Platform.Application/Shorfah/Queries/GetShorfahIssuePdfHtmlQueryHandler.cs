using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Handles <see cref="GetShorfahIssuePdfHtmlQuery"/>. Ports <c>shorfah.ts:622-704</c>.</summary>
public sealed class GetShorfahIssuePdfHtmlQueryHandler : IRequestHandler<GetShorfahIssuePdfHtmlQuery, Result<string>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly ShorfahExportSectionSelector _sectionSelector;

    /// <summary>Initializes a new instance of the <see cref="GetShorfahIssuePdfHtmlQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="sectionSelector">The shared preview/final section selector.</param>
    public GetShorfahIssuePdfHtmlQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, ShorfahExportSectionSelector sectionSelector)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _sectionSelector = sectionSelector;
    }

    /// <inheritdoc />
    public async Task<Result<string>> Handle(GetShorfahIssuePdfHtmlQuery request, CancellationToken cancellationToken)
    {
        ShorfahIssue? issue = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahIssues.Where(i => i.Id == request.IssueId), cancellationToken);
        if (issue is null)
        {
            return Result<string>.Failure("العدد غير موجود");
        }

        List<ShorfahSection> sections = await _sectionSelector.SelectAsync(request.IssueId, request.Preview, cancellationToken);
        return Result<string>.Success(ShorfahIssueHtmlBuilder.Build(issue, sections));
    }
}
