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

    /// <summary>Maps an HTTP method to the RBAC verb it implies (BUSINESS-RULES.md §10.1 method-to-permission mapping).</summary>
    /// <param name="httpMethod">The HTTP method, e.g. <c>GET</c>, <c>POST</c>.</param>
    /// <returns>The implied <see cref="PermissionVerb"/>.</returns>
    public static PermissionVerb FromHttpMethod(string httpMethod)
    {
        return httpMethod.ToUpperInvariant() switch
        {
            "GET" or "HEAD" => PermissionVerb.View,
            "POST" => PermissionVerb.Create,
            "PUT" or "PATCH" => PermissionVerb.Edit,
            "DELETE" => PermissionVerb.Delete,
            _ => PermissionVerb.View,
        };
    }

    /// <summary>Converts the machine permission name stored in <see cref="Permission.Name"/> to the enum, if it maps to one of the 4 policy verbs.</summary>
    /// <param name="permissionName">The lowercase permission name, e.g. <c>view</c>, <c>export</c>.</param>
    /// <returns>The matching verb, or <c>null</c> if the name doesn't map to a policy verb (e.g. <c>export</c>).</returns>
    public static PermissionVerb? TryParseVerb(string permissionName)
    {
        return Enum.TryParse<PermissionVerb>(permissionName, ignoreCase: true, out PermissionVerb verb)
            ? verb
            : null;
    }
}
