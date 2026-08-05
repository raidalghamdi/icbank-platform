using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Handles <see cref="UpdateRolePermissionsCommand"/>.</summary>
public sealed class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="UpdateRolePermissionsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public UpdateRolePermissionsCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        Role? role = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Roles.Where(r => r.Id == request.RoleId), cancellationToken);
        if (role is null)
        {
            return Result<bool>.Failure("role_not_found");
        }

        List<RolePermission> existingGrants = await _queryExecutor.ToListAsync(
            _dbContext.RolePermissions.Where(rp => rp.RoleId == request.RoleId), cancellationToken);
        var beforeSnapshot = existingGrants.Select(g => new { g.PageId, g.PermissionId }).ToList();

        await ReplaceGrantsAsync(request, existingGrants, cancellationToken);

        await _auditLog.RecordAsync(
            request.ActorUserId,
            "role.permissions.replace",
            "Role",
            request.RoleId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: beforeSnapshot,
            after: request.Grants,
            cancellationToken);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Removes the role's existing permission grants and inserts the requested replacement set
    /// in a single change-tracker graph, flushed by one <c>SaveChangesAsync</c> call.
    /// </summary>
    private async Task ReplaceGrantsAsync(
        UpdateRolePermissionsCommand request, List<RolePermission> existingGrants, CancellationToken cancellationToken)
    {
        List<Page> pages = await _queryExecutor.ToListAsync(_dbContext.Pages, cancellationToken);
        List<Permission> permissions = await _queryExecutor.ToListAsync(_dbContext.Permissions, cancellationToken);

        // Why: DEFECT-LOG.md DATA-05 pattern — the old system's delete-then-bulk-insert was two
        // separate, non-transactional statements. Here both the removals and the additions are
        // tracked in the same change-tracker graph and flushed in a single SaveChangesAsync call,
        // so either all of it lands or none of it does.
        foreach (RolePermission grant in existingGrants)
        {
            _dbContext.Remove(grant);
        }

        foreach ((var pageSlug, var permissionName) in request.Grants)
        {
            Page? page = pages.SingleOrDefault(p => p.Slug == pageSlug);
            Permission? permission = permissions.SingleOrDefault(p => p.Name == permissionName);
            if (page is null || permission is null)
            {
                continue;
            }

            _dbContext.Add(new RolePermission { RoleId = request.RoleId, PageId = page.Id, PermissionId = permission.Id });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
