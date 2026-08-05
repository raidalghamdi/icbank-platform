namespace Icbank.Platform.Application.Admin;

/// <summary>Admin-facing user summary row (API-SURFACE.md §5 <c>GET /admin/users</c>).</summary>
/// <param name="Id">The user's id.</param>
/// <param name="Email">The user's email.</param>
/// <param name="Name">The user's display name.</param>
/// <param name="RoleNames">The union of role machine-names the user holds.</param>
/// <param name="IsActive">Whether the account is active.</param>
/// <param name="IsLocked">Whether the account is locked out.</param>
public sealed record UserSummaryDto(int Id, string Email, string Name, IReadOnlyCollection<string> RoleNames, bool IsActive, bool IsLocked);
