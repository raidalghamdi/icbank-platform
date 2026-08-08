using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Gac.News;
using Icbank.Platform.Domain.Gac;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>Handles <see cref="PurgeIrrelevantGacNewsCommand"/>.</summary>
public sealed class PurgeIrrelevantGacNewsCommandHandler
    : IRequestHandler<PurgeIrrelevantGacNewsCommand, Result<PurgeIrrelevantGacNewsResult>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="PurgeIrrelevantGacNewsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="queryExecutor">The async query executor.</param>
    public PurgeIrrelevantGacNewsCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PurgeIrrelevantGacNewsResult>> Handle(
        PurgeIrrelevantGacNewsCommand request, CancellationToken cancellationToken)
    {
        List<GacNewsItem> stored = await _queryExecutor.ToListAsync(_dbContext.GacNewsItems, cancellationToken);

        var removed = 0;
        foreach (GacNewsItem item in stored)
        {
            if (NewsRelevanceFilter.IsRelevant(item.TitleAr, item.BodyAr))
            {
                continue;
            }

            _dbContext.Remove(item);
            removed++;
        }

        if (removed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<PurgeIrrelevantGacNewsResult>.Success(
            new PurgeIrrelevantGacNewsResult(stored.Count, removed));
    }
}
