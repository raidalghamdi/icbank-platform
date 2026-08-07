using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Handles <see cref="GetStyleProfileQuery"/>.</summary>
public sealed class GetStyleProfileQueryHandler : IRequestHandler<GetStyleProfileQuery, Result<StyleProfileDto?>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetStyleProfileQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetStyleProfileQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<StyleProfileDto?>> Handle(GetStyleProfileQuery request, CancellationToken cancellationToken)
    {
        List<StyleProfile> profiles = await _queryExecutor.ToListAsync(_dbContext.StyleProfiles, cancellationToken);
        StyleProfile? latest = profiles.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt).FirstOrDefault();

        return Result<StyleProfileDto?>.Success(latest is null ? null : ToDto(latest));
    }

    private static StyleProfileDto ToDto(StyleProfile profile) => new(
        profile.ToneSummary, profile.AvgParagraphLength, profile.OpenerPatterns, profile.CloserPatterns, profile.RecurringKeywords, profile.QuoteUsage);
}
