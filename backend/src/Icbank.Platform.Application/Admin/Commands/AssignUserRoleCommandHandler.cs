using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>
/// Handles <see cref="AssignUserRoleCommand"/>. Closes SEC-01 in depth: even if the API-layer
/// <c>[Authorize(Policy = "super-admin")]</c> gate were ever misconfigured or bypassed, this
/// handler independently refuses to assign the <c>super_admin</c> role unless the acting caller
/// already holds the super-admin capability — a plain <c>admin</c> can never grant it, to
/// themselves or anyone else.
/// </summary>
public sealed class AssignUserRoleCommandHandler : IRequestHandler<AssignUserRoleCommand, Result<bool>>
{
    private const string SuperAdminRoleName = "super_admin";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="AssignUserRoleCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public AssignUserRoleCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        Role? role = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Roles.Where(r => r.Id == request.RoleId), cancellationToken);
        Result<bool>? roleGuardFailure = ValidateRole(role, request.ActorIsSuperAdmin);
        if (roleGuardFailure is not null || role is null)
        {
            return roleGuardFailure!.Value;
        }

        User? targetUser = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Users.Where(u => u.Id == request.TargetUserId), cancellationToken);
        if (targetUser is null)
        {
            return Result<bool>.Failure("user_not_found");
        }

        var alreadyAssigned = await _queryExecutor.AnyAsync(
            _dbContext.UserRoles.Where(ur => ur.UserId == request.TargetUserId && ur.RoleId == request.RoleId), cancellationToken);
        if (alreadyAssigned)
        {
            return Result<bool>.Success(true);
        }

        _dbContext.Add(new UserRole
        {
            UserId = request.TargetUserId,
            RoleId = request.RoleId,
            AssignedById = request.ActorUserId,
            AssignedAt = DateTime.UtcNow,
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        await RecordAssignmentAuditAsync(request.ActorUserId, request.TargetUserId, role, cancellationToken);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Validates the target role exists and, per SEC-01, that a non-super-admin actor is not
    /// granting the <c>super_admin</c> role to anyone, including themselves.
    /// </summary>
    /// <returns>A failure result if a guard is violated; otherwise <see langword="null"/>.</returns>
    private static Result<bool>? ValidateRole(Role? role, bool actorIsSuperAdmin)
    {
        if (role is null)
        {
            return Result<bool>.Failure("role_not_found");
        }

        // Why: SEC-01 — this is the enforcement point. A plain admin (ActorIsSuperAdmin == false)
        // may never grant super_admin, to anyone, including themselves.
        if (string.Equals(role.Name, SuperAdminRoleName, StringComparison.OrdinalIgnoreCase) && !actorIsSuperAdmin)
        {
            return Result<bool>.Failure("forbidden_super_admin_grant");
        }

        return null;
    }

    /// <summary>Writes the privileged-action audit-log entry for the role assignment.</summary>
    private Task RecordAssignmentAuditAsync(int actorUserId, int targetUserId, Role role, CancellationToken cancellationToken) =>
        _auditLog.RecordAsync(
            actorUserId,
            "user.role.assign",
            "User",
            targetUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { roleId = role.Id, roleName = role.Name },
            cancellationToken);
}
