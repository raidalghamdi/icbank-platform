namespace Icbank.Platform.Application.Shorfah;

/// <summary>The Shorfah section-permission-grant response shape (API-SURFACE.md §19).</summary>
/// <param name="Id">The grant id.</param>
/// <param name="SectionId">The scoped section's id.</param>
/// <param name="UserId">The granted user's id, if scoped by user.</param>
/// <param name="RoleName">The granted role name, if scoped by role.</param>
/// <param name="Permission">The granted permission verb.</param>
public sealed record ShorfahSectionPermissionDto(int Id, int SectionId, int? UserId, string? RoleName, string Permission);
