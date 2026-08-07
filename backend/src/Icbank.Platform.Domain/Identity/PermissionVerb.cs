namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// The 4 RBAC verbs used to compose per-page authorization policy names
/// (DOTNET-CONVENTIONS.md §5.4). <c>Export</c> remains a separate catalogued
/// <see cref="Permission"/> row (DATA-MODEL.md parity) but is not one of the 4 policy-generating
/// verbs the task's 9×18×4 matrix specifies.
/// </summary>
public enum PermissionVerb
{
    /// <summary>Permission to view/read a resource.</summary>
    View = 0,

    /// <summary>Permission to create a resource.</summary>
    Create = 1,

    /// <summary>Permission to edit a resource.</summary>
    Edit = 2,

    /// <summary>Permission to delete a resource.</summary>
    Delete = 3,
}
