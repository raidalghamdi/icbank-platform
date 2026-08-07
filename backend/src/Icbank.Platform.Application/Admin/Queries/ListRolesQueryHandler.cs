using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>Handles <see cref="ListRolesQuery"/>.</summary>
public sealed class ListRolesQueryHandler : IRequestHandler<ListRolesQuery, Result<PagedResult<RoleSummaryDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListRolesQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListRolesQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<RoleSummaryDto>>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        List<Role> total = await _queryExecutor.ToListAsync(_dbContext.Roles, cancellationToken);
        List<Role> page = await _queryExecutor.ToListAsync(
            _dbContext.Roles.OrderBy(r => r.Id).Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize),
            cancellationToken);

        List<UserRole> assignments = await _queryExecutor.ToListAsync(_dbContext.UserRoles, cancellationToken);
        var items = page
            .Select(role => new RoleSummaryDto(role.Id, role.Name, role.NameAr, role.Description, role.IsSystem, assignments.Count(a => a.RoleId == role.Id)))
            .ToList();

        return Result<PagedResult<RoleSummaryDto>>.Success(new PagedResult<RoleSummaryDto>(items, request.Query.Page, request.Query.PageSize, total.Count));
    }
}
