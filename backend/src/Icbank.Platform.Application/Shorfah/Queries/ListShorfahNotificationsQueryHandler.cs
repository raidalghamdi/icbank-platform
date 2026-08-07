using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Handles <see cref="ListShorfahNotificationsQuery"/>. Ports <c>shorfah.ts:1000-1009</c>.</summary>
public sealed class ListShorfahNotificationsQueryHandler : IRequestHandler<ListShorfahNotificationsQuery, Result<PagedResult<ShorfahNotificationDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListShorfahNotificationsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListShorfahNotificationsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ShorfahNotificationDto>>> Handle(ListShorfahNotificationsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ShorfahNotification> ordered = _dbContext.ShorfahNotifications
            .Where(n => n.UserId == request.UserId)
            .OrderByDescending(n => n.CreatedAt);

        List<int> allIds = await _queryExecutor.ToListAsync(ordered.Select(n => n.Id), cancellationToken);

        List<ShorfahNotification> page = await _queryExecutor.ToListAsync(
            ordered.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);

        var items = page
            .Select(n => new ShorfahNotificationDto(n.Id, n.IssueId, n.SectionId, n.Type, n.Title, n.Body, n.Url, n.IsRead, n.CreatedAt))
            .ToList();

        return Result<PagedResult<ShorfahNotificationDto>>.Success(
            new PagedResult<ShorfahNotificationDto>(items, request.Query.Page, request.Query.PageSize, allIds.Count));
    }
}
