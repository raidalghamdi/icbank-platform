using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Auth.Queries;

/// <summary>Handles <see cref="GetCurrentUserQuery"/>.</summary>
public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<AuthenticatedUserDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPermissionResolver _permissionResolver;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetCurrentUserQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="permissionResolver">The effective-permission resolution port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetCurrentUserQueryHandler(IApplicationDbContext dbContext, IPermissionResolver permissionResolver, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _permissionResolver = permissionResolver;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticatedUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        User? user = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Users.Where(u => u.Id == request.UserId), cancellationToken);
        if (user is null)
        {
            return Result<AuthenticatedUserDto>.Failure("user_not_found");
        }

        PermissionResolution resolution = await _permissionResolver.ResolveAsync(user.Id, cancellationToken);

        var dto = new AuthenticatedUserDto(
            user.Id,
            user.Email,
            user.Name,
            resolution.RoleNames,
            resolution.IsSuperAdmin,
            resolution.Permissions,
            user.MustChangePassword);

        return Result<AuthenticatedUserDto>.Success(dto);
    }
}
