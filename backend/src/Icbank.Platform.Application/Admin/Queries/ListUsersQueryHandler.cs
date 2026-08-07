using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>Handles <see cref="ListUsersQuery"/>.</summary>
public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, Result<PagedResult<UserSummaryDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListUsersQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListUsersQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<UserSummaryDto>>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        IQueryable<User> filtered = _dbContext.Users;
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = request.Search.Trim();
            filtered = filtered.Where(u => u.Email.Contains(pattern) || u.Name.Contains(pattern));
        }

        var total = await _queryExecutor.AnyAsync(filtered, cancellationToken)
            ? (await _queryExecutor.ToListAsync(filtered, cancellationToken)).Count
            : 0;

        List<User> page = await _queryExecutor.ToListAsync(
            filtered.OrderBy(u => u.Id).Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize),
            cancellationToken);

        List<UserRole> roleAssignments = await _queryExecutor.ToListAsync(
            _dbContext.UserRoles.Where(ur => page.Select(u => u.Id).Contains(ur.UserId)), cancellationToken);
        List<Role> roles = await _queryExecutor.ToListAsync(_dbContext.Roles, cancellationToken);

        var items = page.Select(user => ToSummary(user, roleAssignments, roles)).ToList();

        return Result<PagedResult<UserSummaryDto>>.Success(
            new PagedResult<UserSummaryDto>(items, request.Query.Page, request.Query.PageSize, total));
    }

    private static UserSummaryDto ToSummary(User user, List<UserRole> roleAssignments, List<Role> roles)
    {
        var roleNames = roleAssignments
            .Where(ur => ur.UserId == user.Id)
            .Join(roles, ur => ur.RoleId, r => r.Id, (_, role) => role.Name)
            .ToList();

        return new UserSummaryDto(user.Id, user.Email, user.Name, roleNames, user.IsActive, user.IsLocked);
    }
}
