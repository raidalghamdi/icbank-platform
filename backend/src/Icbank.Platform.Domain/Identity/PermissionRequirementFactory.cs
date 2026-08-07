namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// Builds the composite <c>{pageSlug}:{verb}</c> policy-name string used both when registering
/// the 72 generated authorization policies at startup and when a controller declares
/// <c>[Authorize(Policy = "shorfah:edit")]</c> (DOTNET-CONVENTIONS.md §5.4).
/// </summary>
public static class PermissionRequirementFactory
{
    /// <summary>Builds the policy name for a given page slug and verb.</summary>
    /// <param name="pageSlug">One of <see cref="PageSlugs"/>.</param>
    /// <param name="verb">One of the 4 RBAC verbs.</param>
    /// <returns>The composite policy name, e.g. <c>shorfah:edit</c>.</returns>
    public static string BuildPolicyName(string pageSlug, PermissionVerb verb) =>
        $"{pageSlug}:{verb.ToString().ToLowerInvariant()}";
}
