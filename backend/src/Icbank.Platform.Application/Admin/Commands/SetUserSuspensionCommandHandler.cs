using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Handles <see cref="SetUserSuspensionCommand"/>. Returns the resulting <c>IsActive</c> value on success.</summary>
public sealed class SetUserSuspensionCommandHandler : IRequestHandler<SetUserSuspensionCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IResourceAuthorizationService _resourceAuthorization;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="SetUserSuspensionCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="resourceAuthorization">The SEC-16 resource-level authorization port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public SetUserSuspensionCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IResourceAuthorizationService resourceAuthorization, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _resourceAuthorization = resourceAuthorization;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(SetUserSuspensionCommand request, CancellationToken cancellationToken)
    {
        if (request.ActorUserId == request.TargetUserId)
        {
            return Result<bool>.Failure("cannot_suspend_self");
        }

        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeUserResourceAsync(
            request.ActorUserId, request.ActorIsSuperAdmin, request.TargetUserId, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return Result<bool>.Failure(authorization.Outcome == ResourceAuthorizationOutcome.NotFound ? "user_not_found" : "forbidden_peer_resource");
        }

        User user = await _queryExecutor.SingleOrDefaultAsync(_dbContext.Users.Where(u => u.Id == request.TargetUserId), cancellationToken)
            ?? throw new InvalidOperationException("User existence was already confirmed by resource authorization.");

        var wasActive = user.IsActive;
        user.IsActive = !user.IsActive;
        user.UpdatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.RecordAsync(
            request.ActorUserId,
            "user.suspension.toggle",
            "User",
            user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: new { IsActive = wasActive },
            after: new { user.IsActive },
            cancellationToken);

        return Result<bool>.Success(user.IsActive);
    }
}
