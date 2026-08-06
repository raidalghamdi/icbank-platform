using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Resolves a user's effective permission set by unioning every role the user holds
/// (BUSINESS-RULES.md §10.1/DOMAIN-PORT-NOTES.md: the old system's <c>.limit(1)</c> single-role
/// bug is fixed here — a user with N roles gets the union of all N roles' grants, not just the
/// first row returned by an unordered query), then applying per-user allow/deny overrides last so
/// a deny always wins over any role grant and an allow always adds even if no role grants it.
/// <c>super_admin</c> is resolved as a distinct capability flag, never inferred from the
/// <c>admin</c> role — this is the enforcement point that closes SEC-01.
/// </summary>
public sealed class PermissionResolver : IPermissionResolver
{
    private readonly IApplicationDbContext _dbContext;

    /// <summary>Initializes a new instance of the <see cref="PermissionResolver"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    public PermissionResolver(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<PermissionResolution> ResolveAsync(int userId, CancellationToken cancellationToken)
    {
        List<string> roleNames = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        var isSuperAdmin = roleNames.Contains(RoleName.SuperAdmin.ToString(), StringComparer.OrdinalIgnoreCase)
            || roleNames.Any(name => string.Equals(name, "super_admin", StringComparison.OrdinalIgnoreCase));

        if (roleNames.Count == 0)
        {
            // Why: BUSINESS-RULES.md §10.2 — a user with zero user_roles rows implicitly gets the
            // "guest" role's (empty) grants, never full/no access by accident.
            roleNames.Add("guest");
        }

        List<string> roleGrants = await _dbContext.RolePermissions
            .Where(rp => roleNames.Contains(rp.Role.Name))
            .Select(rp => rp.Page.Slug + ":" + rp.Permission.Name.ToLowerInvariant())
            .ToListAsync(cancellationToken);

        var effective = new HashSet<string>(roleGrants, StringComparer.OrdinalIgnoreCase);

        List<(string Key, OverrideGrantType GrantType, string? CreatedByName)> overrides = await _dbContext.UserPageOverrides
            .Where(o => o.UserId == userId)
            .OrderBy(o => o.Id)
            .Select(o => new ValueTuple<string, OverrideGrantType, string?>(
                o.Page.Slug + ":" + o.Permission.Name.ToLowerInvariant(),
                o.GrantType,
                o.CreatedByUser != null ? o.CreatedByUser.Name : null))
            .ToListAsync(cancellationToken);

        foreach ((var key, OverrideGrantType grantType, _) in overrides)
        {
            ApplyOverride(effective, key, grantType);
        }

        // Presentational only: the administrator behind the newest override, so the UI can name
        // somebody to ask about access rather than showing a locked item with no recourse. Ordered
        // by Id (not CreatedAt) to match the deterministic ordering used when applying the
        // overrides above, and because Id is monotonic while CreatedAt can tie within a single
        // matrix save that writes several rows in one transaction.
        var accessGrantedBy = overrides
            .Select(entry => entry.CreatedByName)
            .LastOrDefault(name => !string.IsNullOrWhiteSpace(name));

        return new PermissionResolution(roleNames, isSuperAdmin, effective, accessGrantedBy);
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(int userId, string pageSlug, PermissionVerb verb, CancellationToken cancellationToken)
    {
        PermissionResolution resolution = await ResolveAsync(userId, cancellationToken);
        var policyKey = PermissionRequirementFactory.BuildPolicyName(pageSlug, verb);
        return resolution.Permissions.Contains(policyKey);
    }

    private static void ApplyOverride(HashSet<string> effective, string key, OverrideGrantType grantType)
    {
        if (grantType == OverrideGrantType.Deny)
        {
            effective.Remove(key);
            return;
        }

        effective.Add(key);
    }
}
