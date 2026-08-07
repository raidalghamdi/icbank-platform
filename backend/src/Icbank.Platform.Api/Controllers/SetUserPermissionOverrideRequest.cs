namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>PUT /api/v1/admin/matrix/user-override</c>.</summary>
/// <param name="UserId">The user the override applies to.</param>
/// <param name="PageSlug">The page the override scopes to.</param>
/// <param name="PermName">The permission verb the override scopes to.</param>
/// <param name="GrantType"><c>"allow"</c>, <c>"deny"</c>, or <c>null</c> to clear.</param>
public sealed record SetUserPermissionOverrideRequest(int UserId, string PageSlug, string PermName, string? GrantType);
