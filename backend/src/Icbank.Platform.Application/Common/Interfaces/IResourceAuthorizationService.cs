namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Port that closes SEC-16 (DEFECT-LOG.md: "IDOR-adjacent: admin/matrix routes trust
/// client-supplied numeric IDs with only role-level checks"). A caller passing role/permission
/// authorization is necessary but not sufficient — this service additionally confirms the
/// resource identified by a client-supplied id actually exists (so a random/guessed id doesn't
/// silently succeed against a different or non-existent row) and, where the resource has an
/// owner/tenant concept, that the acting user is entitled to reach that specific row rather than
/// merely "some row of that type". Every admin handler in this work package that accepts a
/// resource id calls this before mutating anything (R-BE-078: "every query is scoped to
/// tenant_id/owner_id or an equivalent policy check to prevent IDOR").
/// </summary>
public interface IResourceAuthorizationService
{
    /// <summary>
    /// Confirms a targeted user row exists and, for non-super-admin actors, that the actor is not
    /// attempting to act on a super-admin peer's account (a lesser admin scoping their own
    /// admin_panel:* grant onto a higher-privileged account it was never meant to reach).
    /// </summary>
    /// <param name="actorUserId">The id of the user performing the action.</param>
    /// <param name="actorIsSuperAdmin">Whether the actor holds the super-admin capability.</param>
    /// <param name="targetUserId">The client-supplied target user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ResourceAuthorizationResult"/> describing whether the target exists and is
    /// reachable by this actor.
    /// </returns>
    Task<ResourceAuthorizationResult> AuthorizeUserResourceAsync(
        int actorUserId, bool actorIsSuperAdmin, int targetUserId, CancellationToken cancellationToken);

    /// <summary>Confirms a targeted role row exists and, for custom (non-system) roles, is a legitimate target for mutation.</summary>
    /// <param name="roleId">The client-supplied role id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ResourceAuthorizationResult"/> describing whether the role exists.</returns>
    Task<ResourceAuthorizationResult> AuthorizeRoleResourceAsync(int roleId, CancellationToken cancellationToken);
}
