namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>PUT /api/v1/admin/roles/{roleId}/permissions</c>.</summary>
/// <param name="Permissions">The full replacement grant set: page slug → the verb names granted for that page.</param>
public sealed record UpdateRolePermissionsRequest(IReadOnlyDictionary<string, IReadOnlyCollection<string>> Permissions);
