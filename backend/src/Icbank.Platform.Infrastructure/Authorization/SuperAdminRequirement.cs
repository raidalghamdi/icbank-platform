using Microsoft.AspNetCore.Authorization;

namespace Icbank.Platform.Infrastructure.Authorization;

/// <summary>
/// Marker requirement for the distinct super-admin capability (closes SEC-01). Registered as the
/// <c>"super-admin"</c> policy — the only policy that grants role assignment, permission-matrix
/// edits, and elevation to <c>super_admin</c> itself. A plain <c>admin</c> never satisfies this
/// requirement, no matter what page/verb permissions it holds.
/// </summary>
public sealed class SuperAdminRequirement : IAuthorizationRequirement
{
    /// <summary>Gets the singleton instance.</summary>
    public static SuperAdminRequirement Instance { get; } = new();
}
