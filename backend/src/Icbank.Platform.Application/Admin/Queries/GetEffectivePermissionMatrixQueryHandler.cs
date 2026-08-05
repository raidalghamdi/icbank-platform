using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>Handles <see cref="GetEffectivePermissionMatrixQuery"/>, reusing <see cref="IPermissionResolver"/> so this view can never drift from the actual authorization decision path.</summary>
public sealed class GetEffectivePermissionMatrixQueryHandler : IRequestHandler<GetEffectivePermissionMatrixQuery, Result<EffectivePermissionMatrixDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IPermissionResolver _permissionResolver;

    /// <summary>Initializes a new instance of the <see cref="GetEffectivePermissionMatrixQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="permissionResolver">The shared effective-permission resolution port.</param>
    public GetEffectivePermissionMatrixQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IPermissionResolver permissionResolver)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _permissionResolver = permissionResolver;
    }

    /// <inheritdoc />
    public async Task<Result<EffectivePermissionMatrixDto>> Handle(GetEffectivePermissionMatrixQuery request, CancellationToken cancellationToken)
    {
        List<Page> pages = await _queryExecutor.ToListAsync(_dbContext.Pages.OrderBy(p => p.SortOrder), cancellationToken);
        List<Permission> permissions = await _queryExecutor.ToListAsync(_dbContext.Permissions, cancellationToken);

        List<User> allUsers = await _queryExecutor.ToListAsync(_dbContext.Users.OrderBy(u => u.Id), cancellationToken);
        var pageOfUsers = allUsers.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize).ToList();

        var rows = new List<UserEffectivePermissionsDto>();
        foreach (User user in pageOfUsers)
        {
            PermissionResolution resolution = await _permissionResolver.ResolveAsync(user.Id, cancellationToken);
            var grantsByPage = pages.ToDictionary(
                page => page.Slug,
                page => (IReadOnlyCollection<string>)permissions
                    .Where(permission => resolution.Permissions.Contains(page.Slug + ":" + permission.Name.ToLowerInvariant()))
                    .Select(permission => permission.Name)
                    .ToList());

            rows.Add(new UserEffectivePermissionsDto(user.Id, user.Email, resolution.RoleNames, grantsByPage));
        }

        var pagedUsers = new PagedResult<UserEffectivePermissionsDto>(rows, request.Query.Page, request.Query.PageSize, allUsers.Count);
        return Result<EffectivePermissionMatrixDto>.Success(new EffectivePermissionMatrixDto(
            pages.Select(p => p.Slug).ToList(), permissions.Select(p => p.Name).ToList(), pagedUsers));
    }
}
