using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Domain.Shorfah;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.Infrastructure.Shorfah;

/// <summary>
/// Default <see cref="IShorfahSectionAccessService"/> implementation, ported verbatim from
/// <c>canAccessSection()</c> (BUSINESS-RULES.md §1.4, <c>shorfah.ts:25-40</c>): <c>super_admin</c>/
/// <c>admin</c> always bypass; otherwise a row in <c>shorfah_section_permissions</c> must match
/// either the caller's user id or one of the caller's role names, for the exact permission tier
/// requested (permissions are not hierarchical -- holding <c>Contribute</c> does not imply <c>View</c>).
/// </summary>
public sealed class ShorfahSectionAccessService : IShorfahSectionAccessService
{
    private static readonly string[] BypassRoles = { "admin", "super_admin" };

    private readonly IApplicationDbContext _dbContext;

    /// <summary>Initializes a new instance of the <see cref="ShorfahSectionAccessService"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    public ShorfahSectionAccessService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<bool> CanAccessSectionAsync(int userId, int sectionId, ShorfahSectionAccessTier permission, CancellationToken cancellationToken)
    {
        List<string> roleNames = await RoleNamesForAsync(userId, cancellationToken);

        if (HasBypassRole(roleNames))
        {
            return true;
        }

        var permissionVerb = (ShorfahPermissionVerb)(int)permission;
        return await _dbContext.ShorfahSectionPermissions
            .Where(p => p.SectionId == sectionId && p.Permission == permissionVerb)
            .AnyAsync(p => p.UserId == userId || (p.RoleName != null && roleNames.Contains(p.RoleName)), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken)
    {
        List<string> roleNames = await RoleNamesForAsync(userId, cancellationToken);
        return HasBypassRole(roleNames);
    }

    private static bool HasBypassRole(IEnumerable<string> roleNames) =>
        roleNames.Any(name => BypassRoles.Contains(name, StringComparer.OrdinalIgnoreCase));

    private Task<List<string>> RoleNamesForAsync(int userId, CancellationToken cancellationToken) =>
        _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);
}
