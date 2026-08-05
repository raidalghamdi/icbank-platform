using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.Infrastructure.Security;

/// <summary>
/// Default <see cref="IResourceAuthorizationService"/> implementation (closes SEC-16). Every
/// check here does two things a bare <c>[Authorize(Policy=...)]</c> attribute cannot: (1) proves
/// the client-supplied id actually refers to a live row, so a handler never silently no-ops or
/// throws an unhandled null-reference on a guessed/stale id, and (2) enforces the one
/// resource-level ownership rule this domain has today — a non-super-admin actor may never reach
/// a super-admin peer's account, even though both pass the same <c>admin_panel:*</c> role-level
/// policy check.
/// </summary>
public sealed class ResourceAuthorizationService : IResourceAuthorizationService
{
    private const string SuperAdminRoleName = "super_admin";

    private readonly IApplicationDbContext _dbContext;

    /// <summary>Initializes a new instance of the <see cref="ResourceAuthorizationService"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    public ResourceAuthorizationService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<ResourceAuthorizationResult> AuthorizeUserResourceAsync(
        int actorUserId, bool actorIsSuperAdmin, int targetUserId, CancellationToken cancellationToken)
    {
        var targetExists = await _dbContext.Users.AnyAsync(u => u.Id == targetUserId, cancellationToken);
        if (!targetExists)
        {
            return ResourceAuthorizationResult.NotFound;
        }

        if (actorIsSuperAdmin || actorUserId == targetUserId)
        {
            return ResourceAuthorizationResult.Authorized;
        }

        // Why: SEC-16 — a plain admin passes the coarse admin_panel:* role check the same as a
        // super-admin, but must never be able to mutate a super-admin peer's account by simply
        // supplying that account's numeric id (the resource-level check the role check alone
        // cannot express).
        var targetIsSuperAdmin = await _dbContext.UserRoles
            .Where(ur => ur.UserId == targetUserId)
            .Select(ur => ur.Role.Name)
            .AnyAsync(name => name == SuperAdminRoleName, cancellationToken);

        return targetIsSuperAdmin ? ResourceAuthorizationResult.ForbiddenPeer : ResourceAuthorizationResult.Authorized;
    }

    /// <inheritdoc />
    public async Task<ResourceAuthorizationResult> AuthorizeRoleResourceAsync(int roleId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
        return exists ? ResourceAuthorizationResult.Authorized : ResourceAuthorizationResult.NotFound;
    }

    /// <inheritdoc />
    public async Task<ResourceAuthorizationResult> AuthorizeShorfahIssueResourceAsync(int issueId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.ShorfahIssues.AnyAsync(i => i.Id == issueId, cancellationToken);
        return exists ? ResourceAuthorizationResult.Authorized : ResourceAuthorizationResult.NotFound;
    }
}
