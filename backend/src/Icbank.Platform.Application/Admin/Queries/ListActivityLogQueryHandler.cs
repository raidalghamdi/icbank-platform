using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>Handles <see cref="ListActivityLogQuery"/>.</summary>
public sealed class ListActivityLogQueryHandler : IRequestHandler<ListActivityLogQuery, Result<PagedResult<ActivityLogEntryDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListActivityLogQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListActivityLogQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ActivityLogEntryDto>>> Handle(ListActivityLogQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ActivityLog> filtered = ApplyFilters(_dbContext.ActivityLogs, request);

        List<ActivityLog> allMatches = await _queryExecutor.ToListAsync(filtered, cancellationToken);
        var page = allMatches
            .OrderByDescending(log => log.CreatedAt)
            .Skip((request.Query.Page - 1) * request.Query.PageSize)
            .Take(request.Query.PageSize)
            .ToList();

        List<User> users = await _queryExecutor.ToListAsync(_dbContext.Users, cancellationToken);
        var items = page.Select(log => ToDto(log, users)).ToList();

        return Result<PagedResult<ActivityLogEntryDto>>.Success(new PagedResult<ActivityLogEntryDto>(items, request.Query.Page, request.Query.PageSize, allMatches.Count));
    }

    private static IQueryable<ActivityLog> ApplyFilters(IQueryable<ActivityLog> query, ListActivityLogQuery request)
    {
        if (request.UserId is not null)
        {
            query = query.Where(log => log.UserId == request.UserId);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(log => log.Action == request.Action);
        }

        if (request.DateFrom is not null)
        {
            query = query.Where(log => log.CreatedAt >= request.DateFrom);
        }

        if (request.DateTo is not null)
        {
            query = query.Where(log => log.CreatedAt <= request.DateTo);
        }

        return query;
    }

    private static ActivityLogEntryDto ToDto(ActivityLog log, List<User> users)
    {
        var email = users.SingleOrDefault(u => u.Id == log.UserId)?.Email;
        return new ActivityLogEntryDto(log.Id, log.UserId, email, log.Action, log.EntityType, log.EntityId, log.IpAddress, log.CreatedAt);
    }
}
