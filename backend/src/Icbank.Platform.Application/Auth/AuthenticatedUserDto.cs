using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Application.Auth;

/// <summary>Public-facing user profile plus effective permissions, returned by login/refresh/me (API-SURFACE.md §2).</summary>
/// <param name="Id">The user's id.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Name">The user's display name.</param>
/// <param name="Role">The deterministic legacy-compatible role selected from <paramref name="RoleNames"/>.</param>
/// <param name="RoleNames">The complete union of role machine-names the user holds.</param>
/// <param name="IsSuperAdmin">Whether the user holds the distinct super-admin capability.</param>
/// <param name="Permissions">Effective permissions grouped as <c>{ pageSlug: [verb, ...] }</c> for the legacy frontend.</param>
/// <param name="MustChangePassword">Whether the user must change their password before continuing (forced first-login reset).</param>
/// <param name="Title">The user's job title, or <c>null</c> if unset. Presentational only.</param>
/// <param name="Department">The user's department, or <c>null</c> if unset. Presentational only.</param>
/// <param name="AccessGrantedBy">
/// The display name of the administrator who most recently tailored this user's access, or
/// <c>null</c> when their access derives purely from their roles. Presentational only — it lets the
/// dashboard tell a user who to ask about a locked area instead of showing a dead end. Never an
/// input to an authorization decision.
/// </param>
public sealed record AuthenticatedUserDto(
    int Id,
    string Email,
    string Name,
    string Role,
    IReadOnlyCollection<string> RoleNames,
    bool IsSuperAdmin,
    IReadOnlyDictionary<string, IReadOnlyCollection<string>> Permissions,
    bool MustChangePassword,
    string? Title = null,
    string? Department = null,
    string? AccessGrantedBy = null)
{
    /// <summary>
    /// Converts the authorization resolver's internal <c>pageSlug:verb</c> set to the historical
    /// frontend contract, while retaining the complete multi-role union separately in
    /// <see cref="RoleNames"/>.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="resolution">The user's effective role and permission resolution.</param>
    /// <param name="mustChangePassword">Whether the user must reset their password.</param>
    /// <returns>A response DTO compatible with the original Node frontend.</returns>
    public static AuthenticatedUserDto Create(User user, PermissionResolution resolution, bool mustChangePassword)
    {
        IReadOnlyCollection<string> roleNames = resolution.RoleNames
            .OrderBy(roleName => roleName, StringComparer.Ordinal)
            .ToArray();

        return new AuthenticatedUserDto(
            user.Id,
            user.Email,
            user.Name,
            SelectCompatibilityRole(roleNames),
            roleNames,
            resolution.IsSuperAdmin,
            GroupPermissions(resolution.Permissions),
            mustChangePassword,
            user.Title,
            user.Department,
            resolution.AccessGrantedBy);
    }

    private static Dictionary<string, IReadOnlyCollection<string>> GroupPermissions(
        IReadOnlyCollection<string> permissions)
    {
        var grouped = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var permission in permissions)
        {
            var separatorIndex = permission.IndexOf(':', StringComparison.Ordinal);
            if (separatorIndex <= 0 || separatorIndex == permission.Length - 1)
            {
                continue;
            }

            var pageSlug = permission[..separatorIndex];
            var verb = permission[(separatorIndex + 1)..];
            if (!grouped.TryGetValue(pageSlug, out SortedSet<string>? verbs))
            {
                verbs = new SortedSet<string>(StringComparer.Ordinal);
                grouped.Add(pageSlug, verbs);
            }

            verbs.Add(verb);
        }

        return grouped.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyCollection<string>)pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    private static string SelectCompatibilityRole(IReadOnlyCollection<string> roleNames)
    {
        // The original Node implementation exposed one arbitrary role because its SQL query used
        // LIMIT 1. The .NET API deliberately retains every role and resolves the union. This field
        // exists only for legacy UI checks, so prefer the two role names that unlock legacy UI
        // affordances; API authorization still uses RoleNames/effective permissions.
        if (roleNames.Contains("super_admin", StringComparer.OrdinalIgnoreCase))
        {
            return "super_admin";
        }

        if (roleNames.Contains("admin", StringComparer.OrdinalIgnoreCase))
        {
            return "admin";
        }

        return roleNames.FirstOrDefault() ?? "guest";
    }
}
