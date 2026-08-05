namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Port for the per-section permission tiers (BUSINESS-RULES.md §1.4 <c>canAccessSection()</c>):
/// <c>view</c>/<c>contribute</c>/<c>review</c>/<c>approve</c>, each granted per-section to either
/// a specific user id or a role name via <c>shorfah_section_permissions</c>. <c>super_admin</c>
/// and <c>admin</c> always bypass, matching the Node source's global RBAC bypass pattern verbatim.
/// </summary>
public interface IShorfahSectionAccessService
{
    /// <summary>Determines whether the given user may exercise the given permission tier on the given section.</summary>
    /// <param name="userId">The acting user's id.</param>
    /// <param name="sectionId">The section being accessed.</param>
    /// <param name="permission">The permission tier being checked.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the user is entitled to exercise <paramref name="permission"/> on the section.</returns>
    Task<bool> CanAccessSectionAsync(int userId, int sectionId, ShorfahSectionAccessTier permission, CancellationToken cancellationToken);

    /// <summary>Determines whether the given user holds the <c>admin</c> or <c>super_admin</c> role (the metadata/SLA field-tier gate in BUSINESS-RULES.md §1.4).</summary>
    /// <param name="userId">The acting user's id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the user is an admin or super-admin.</returns>
    Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken);
}
