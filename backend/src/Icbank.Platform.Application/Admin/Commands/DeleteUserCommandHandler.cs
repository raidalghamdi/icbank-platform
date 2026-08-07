using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Handles <see cref="DeleteUserCommand"/>: soft-delete, self-delete guard, SEC-16 resource check.</summary>
public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IResourceAuthorizationService _resourceAuthorization;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="DeleteUserCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="resourceAuthorization">The SEC-16 resource-level authorization port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public DeleteUserCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IResourceAuthorizationService resourceAuthorization, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _resourceAuthorization = resourceAuthorization;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (request.ActorUserId == request.TargetUserId)
        {
            return Result<bool>.Failure("cannot_delete_self");
        }

        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeUserResourceAsync(
            request.ActorUserId, request.ActorIsSuperAdmin, request.TargetUserId, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return Result<bool>.Failure(authorization.Outcome == ResourceAuthorizationOutcome.NotFound ? "user_not_found" : "forbidden_peer_resource");
        }

        User user = await _queryExecutor.SingleOrDefaultAsync(_dbContext.Users.Where(u => u.Id == request.TargetUserId), cancellationToken)
            ?? throw new InvalidOperationException("User existence was already confirmed by resource authorization.");

        // Why: R-BE-023 — soft-delete only, never DbSet.Remove on a business table.
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        user.UpdatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.RecordAsync(
            request.ActorUserId,
            "user.delete",
            "User",
            user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: new { user.Email, IsActive = true },
            after: new { DeletedAt = user.DeletedAt, IsActive = false },
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
