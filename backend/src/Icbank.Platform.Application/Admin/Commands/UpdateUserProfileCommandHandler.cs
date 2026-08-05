using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Handles <see cref="UpdateUserProfileCommand"/>, gated by the SEC-16 resource-level authorization check.</summary>
public sealed class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result<UserDetailDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IResourceAuthorizationService _resourceAuthorization;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="UpdateUserProfileCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="resourceAuthorization">The SEC-16 resource-level authorization port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public UpdateUserProfileCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IResourceAuthorizationService resourceAuthorization, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _resourceAuthorization = resourceAuthorization;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<UserDetailDto>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
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

        var before = new { user.Name, user.Title, user.Department, user.Email };

        user.Name = request.Name ?? user.Name;
        user.Title = request.Title ?? user.Title;
        user.Department = request.Department ?? user.Department;
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? user.Email : request.Email.Trim().ToLowerInvariant();
        user.UpdatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.RecordAsync(
            request.ActorUserId,
            "user.profile.update",
            "User",
            user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before,
            after: new { user.Name, user.Title, user.Department, user.Email },
            cancellationToken);

        List<string> roleNames = await _queryExecutor.ToListAsync(
            _dbContext.UserRoles.Where(ur => ur.UserId == user.Id).Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name),
            cancellationToken);

        return Result<UserDetailDto>.Success(new UserDetailDto(
            user.Id, user.Email, user.Name, user.Title, user.Department, roleNames, user.IsActive, user.IsLocked, user.MustChangePassword, user.LastLogin, user.CreatedAt));
    }
}
