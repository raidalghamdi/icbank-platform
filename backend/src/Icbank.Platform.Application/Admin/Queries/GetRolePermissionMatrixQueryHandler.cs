using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>Handles <see cref="GetRolePermissionMatrixQuery"/>.</summary>
public sealed class GetRolePermissionMatrixQueryHandler : IRequestHandler<GetRolePermissionMatrixQuery, Result<RolePermissionMatrixDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IResourceAuthorizationService _resourceAuthorization;

    /// <summary>Initializes a new instance of the <see cref="GetRolePermissionMatrixQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="resourceAuthorization">The SEC-16 resource-level authorization port.</param>
    public GetRolePermissionMatrixQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IResourceAuthorizationService resourceAuthorization)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _resourceAuthorization = resourceAuthorization;
    }

    /// <inheritdoc />
    public async Task<Result<RolePermissionMatrixDto>> Handle(GetRolePermissionMatrixQuery request, CancellationToken cancellationToken)
    {
        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeRoleResourceAsync(request.RoleId, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return Result<RolePermissionMatrixDto>.Failure("role_not_found");
        }

        List<Page> pages = await _queryExecutor.ToListAsync(_dbContext.Pages.OrderBy(p => p.SortOrder), cancellationToken);
        List<Permission> permissions = await _queryExecutor.ToListAsync(_dbContext.Permissions, cancellationToken);
        List<RolePermission> grants = await _queryExecutor.ToListAsync(_dbContext.RolePermissions.Where(rp => rp.RoleId == request.RoleId), cancellationToken);

        var grantsByPage = pages.ToDictionary(
            page => page.Slug,
            page => (IReadOnlyCollection<string>)grants
                .Where(g => g.PageId == page.Id)
                .Join(permissions, g => g.PermissionId, p => p.Id, (_, permission) => permission.Name)
                .ToList());

        return Result<RolePermissionMatrixDto>.Success(new RolePermissionMatrixDto(
            pages.Select(p => p.Slug).ToList(),
            permissions.Select(p => p.Name).ToList(),
            grantsByPage));
    }
}
