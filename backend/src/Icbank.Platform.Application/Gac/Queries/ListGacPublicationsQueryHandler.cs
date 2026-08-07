using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Gac;
using MediatR;

namespace Icbank.Platform.Application.Gac.Queries;

/// <summary>Handles <see cref="ListGacPublicationsQuery"/>.</summary>
public sealed class ListGacPublicationsQueryHandler : IRequestHandler<ListGacPublicationsQuery, Result<PagedResult<GacPublicationDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListGacPublicationsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListGacPublicationsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<GacPublicationDto>>> Handle(ListGacPublicationsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<GacPublication> query = _dbContext.GacPublications.Where(p => p.Status == GacPublicationStatus.Published);

        if (!string.IsNullOrWhiteSpace(request.Category) &&
            Enum.TryParse(request.Category, ignoreCase: true, out GacPublicationCategory category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(request.Language) &&
            Enum.TryParse(request.Language, ignoreCase: true, out GacPublicationLanguage language))
        {
            query = query.Where(p => p.Language == language);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var pattern = request.SearchText.Trim();
            query = query.Where(p =>
                p.TitleAr.Contains(pattern) ||
                (p.TitleEn != null && p.TitleEn.Contains(pattern)) ||
                (p.DescriptionAr != null && p.DescriptionAr.Contains(pattern)));
        }

        query = query.OrderByDescending(p => p.DisplayOrder).ThenByDescending(p => p.PublishedAt);

        List<int> allIds = await _queryExecutor.ToListAsync(query.Select(p => p.Id), cancellationToken);
        var total = allIds.Count;

        List<GacPublication> page = await _queryExecutor.ToListAsync(
            query.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize),
            cancellationToken);

        var items = page.Select(ToDto).ToList();
        return Result<PagedResult<GacPublicationDto>>.Success(new PagedResult<GacPublicationDto>(items, request.Query.Page, request.Query.PageSize, total));
    }

    private static GacPublicationDto ToDto(GacPublication p) => new(
        p.Id,
        p.TitleAr,
        p.TitleEn,
        p.Category.ToString(),
        p.Language.ToString(),
        p.DescriptionAr,
        p.DescriptionEn,
        p.FileUrl,
        p.FileSizeBytes,
        p.PageCount,
        p.Tags,
        p.SourceDomain.ToString(),
        p.Status.ToString(),
        p.DisplayOrder,
        p.PublishedAt);
}
