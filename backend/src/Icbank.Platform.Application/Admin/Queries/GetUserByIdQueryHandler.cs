using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>
/// Handles <see cref="GetUserByIdQuery"/>. Applies the SEC-16 resource-level authorization check
/// before returning any data — a plain admin passing the coarse <c>admin_panel:view</c> policy
/// must still be refused a super-admin peer's record.
/// </summary>
public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDetailDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IResourceAuthorizationService _resourceAuthorization;

    /// <summary>Initializes a new instance of the <see cref="GetUserByIdQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="resourceAuthorization">The SEC-16 resource-level authorization port.</param>
    public GetUserByIdQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IResourceAuthorizationService resourceAuthorization)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _resourceAuthorization = resourceAuthorization;
    }

    /// <inheritdoc />
    public async Task<Result<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeUserResourceAsync(
            request.ActorUserId, request.ActorIsSuperAdmin, request.TargetUserId, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return Result<UserDetailDto>.Failure(
                authorization.Outcome == ResourceAuthorizationOutcome.NotFound ? "user_not_found" : "forbidden_peer_resource");
        }

        User user = await _queryExecutor.SingleOrDefaultAsync(_dbContext.Users.Where(u => u.Id == request.TargetUserId), cancellationToken)
            ?? throw new InvalidOperationException("User existence was already confirmed by resource authorization.");

        List<string> roleNames = await _queryExecutor.ToListAsync(
            _dbContext.UserRoles.Where(ur => ur.UserId == user.Id).Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name),
            cancellationToken);

        return Result<UserDetailDto>.Success(new UserDetailDto(
            user.Id, user.Email, user.Name, user.Title, user.Department, roleNames, user.IsActive, user.IsLocked, user.MustChangePassword, user.LastLogin, user.CreatedAt));
    }
}
