using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Handles <see cref="SetUserPermissionOverrideCommand"/>.</summary>
public sealed class SetUserPermissionOverrideCommandHandler : IRequestHandler<SetUserPermissionOverrideCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="SetUserPermissionOverrideCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public SetUserPermissionOverrideCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(SetUserPermissionOverrideCommand request, CancellationToken cancellationToken)
    {
        // Why: SEC-16 — even though this endpoint is already super-admin-only at the policy
        // level, the client-supplied target/page/permission ids must still be proven to exist
        // before use; a guessed/stale id must fail closed, not silently no-op or throw.
        var targetExists = await _queryExecutor.AnyAsync(_dbContext.Users.Where(u => u.Id == request.TargetUserId), cancellationToken);
        if (!targetExists)
        {
            return Result<bool>.Failure("user_not_found");
        }

        Page? page = await _queryExecutor.SingleOrDefaultAsync(_dbContext.Pages.Where(p => p.Slug == request.PageSlug), cancellationToken);
        Permission? permission = await _queryExecutor.SingleOrDefaultAsync(_dbContext.Permissions.Where(p => p.Name == request.PermissionName), cancellationToken);
        if (page is null || permission is null)
        {
            return Result<bool>.Failure("page_or_permission_not_found");
        }

        UserPageOverride? existing = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.UserPageOverrides.Where(o => o.UserId == request.TargetUserId && o.PageId == page.Id && o.PermissionId == permission.Id),
            cancellationToken);

        var before = existing is null ? null : new { existing.GrantType };
        ApplyOverride(request, page, permission, existing);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.RecordAsync(
            request.ActorUserId,
            "user.permission_override.set",
            "UserPageOverride",
            request.TargetUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before,
            after: new { request.PageSlug, request.PermissionName, request.GrantType },
            cancellationToken);

        return Result<bool>.Success(true);
    }

    private void ApplyOverride(SetUserPermissionOverrideCommand request, Page page, Permission permission, UserPageOverride? existing)
    {
        var actorId = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (request.GrantType is null)
        {
            if (existing is not null)
            {
                _dbContext.Remove(existing);
            }

            return;
        }

        OverrideGrantType grantType = Enum.Parse<OverrideGrantType>(request.GrantType, ignoreCase: true);
        if (existing is null)
        {
            _dbContext.Add(new UserPageOverride
            {
                UserId = request.TargetUserId,
                PageId = page.Id,
                PermissionId = permission.Id,
                GrantType = grantType,
                CreatedByUserId = request.ActorUserId,
                CreatedBy = actorId,
            });
            return;
        }

        existing.GrantType = grantType;
        existing.UpdatedBy = actorId;
    }
}
