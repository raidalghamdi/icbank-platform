using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Handles <see cref="DeleteRoleCommand"/>.</summary>
public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IResourceAuthorizationService _resourceAuthorization;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="DeleteRoleCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="resourceAuthorization">The SEC-16 resource-level authorization port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public DeleteRoleCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IResourceAuthorizationService resourceAuthorization, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _resourceAuthorization = resourceAuthorization;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeRoleResourceAsync(request.RoleId, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return Result<bool>.Failure("role_not_found");
        }

        Role role = await _queryExecutor.SingleOrDefaultAsync(_dbContext.Roles.Where(r => r.Id == request.RoleId), cancellationToken)
            ?? throw new InvalidOperationException("Role existence was already confirmed by resource authorization.");

        if (role.IsSystem)
        {
            return Result<bool>.Failure("cannot_delete_system_role");
        }

        var hasAssignedUsers = await _queryExecutor.AnyAsync(_dbContext.UserRoles.Where(ur => ur.RoleId == request.RoleId), cancellationToken);
        if (hasAssignedUsers)
        {
            return Result<bool>.Failure("role_has_assigned_users");
        }

        // Why: R-BE-023 — soft-delete only, never DbSet.Remove on a business table.
        role.DeletedAt = DateTime.UtcNow;
        role.UpdatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.RecordAsync(
            request.ActorUserId,
            "role.delete",
            "Role",
            role.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: new { role.Name, DeletedAt = (DateTime?)null },
            after: new { DeletedAt = role.DeletedAt },
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
