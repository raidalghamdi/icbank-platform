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

    /// <summary>
    /// Confirms a targeted Shorfah issue row exists (Wave 4a: SEC-16 applied to the issue
    /// lifecycle). The Shorfah issue aggregate has no owner/tenant concept beyond existence, so
    /// this check is a pure existence guard -- a guessed/stale id fails closed with 404 rather
    /// than a handler silently no-oping or throwing an unhandled null-reference.
    /// </summary>
    /// <param name="issueId">The client-supplied issue id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ResourceAuthorizationResult"/> describing whether the issue exists.</returns>
    Task<ResourceAuthorizationResult> AuthorizeShorfahIssueResourceAsync(int issueId, CancellationToken cancellationToken);

    /// <summary>Confirms a targeted Shorfah section row exists (Wave 4b: SEC-16 applied to the section workflow).</summary>
    /// <param name="sectionId">The client-supplied section id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ResourceAuthorizationResult"/> describing whether the section exists.</returns>
    Task<ResourceAuthorizationResult> AuthorizeShorfahSectionResourceAsync(int sectionId, CancellationToken cancellationToken);

    /// <summary>Confirms a targeted Shorfah section-media row exists (Wave 4b: SEC-16/SEC-17 -- media is per-section data).</summary>
    /// <param name="mediaId">The client-supplied media id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ResourceAuthorizationResult"/> describing whether the media row exists.</returns>
    Task<ResourceAuthorizationResult> AuthorizeShorfahMediaResourceAsync(int mediaId, CancellationToken cancellationToken);

    /// <summary>Confirms a targeted Shorfah assignment row exists (Wave 4b: SEC-16).</summary>
    /// <param name="assignmentId">The client-supplied assignment id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ResourceAuthorizationResult"/> describing whether the assignment exists.</returns>
    Task<ResourceAuthorizationResult> AuthorizeShorfahAssignmentResourceAsync(int assignmentId, CancellationToken cancellationToken);

    /// <summary>Confirms a targeted Shorfah section-permission grant row exists (Wave 4b: SEC-16).</summary>
    /// <param name="permissionId">The client-supplied permission-grant id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ResourceAuthorizationResult"/> describing whether the grant exists.</returns>
    Task<ResourceAuthorizationResult> AuthorizeShorfahPermissionResourceAsync(int permissionId, CancellationToken cancellationToken);

    /// <summary>
    /// Confirms a targeted notification row exists AND belongs to the acting user (Wave 4b: the
    /// primary IDOR surface named by the task brief -- a user must never read or mark-read another
    /// user's notification). Unlike the other Shorfah checks above, this one is ownership-scoped,
    /// not a pure existence guard: a notification belonging to a different user resolves to
    /// <see cref="ResourceAuthorizationOutcome.NotFound"/> (not <c>ForbiddenPeer</c>), so a probing
    /// caller cannot distinguish "belongs to someone else" from "does not exist".
    /// </summary>
    /// <param name="actorUserId">The authenticated caller's id.</param>
    /// <param name="notificationId">The client-supplied notification id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ResourceAuthorizationResult"/> describing whether the notification exists and belongs to the caller.</returns>
    Task<ResourceAuthorizationResult> AuthorizeShorfahNotificationResourceAsync(int actorUserId, int notificationId, CancellationToken cancellationToken);
}
