namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/shorfah/sections/{sectionId}/permissions</c>.</summary>
/// <param name="UserId">The granted user's id, mutually exclusive with <paramref name="RoleName"/>.</param>
/// <param name="RoleName">The granted role name, mutually exclusive with <paramref name="UserId"/>.</param>
/// <param name="Permission">The permission verb being granted.</param>
public sealed record GrantShorfahSectionPermissionRequest(int? UserId, string? RoleName, string Permission);
