using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Api.Extensions;
using Icbank.Platform.Application.Admin;
using Icbank.Platform.Application.Admin.Commands;
using Icbank.Platform.Application.Admin.Queries;
using Icbank.Platform.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Admin user/role/permission-matrix management (API-SURFACE.md §5). Every mutating action here
/// writes a dedicated audit-log entry (task requirement 5). Role assignment and permission-matrix
/// edits additionally require the distinct <c>super-admin</c> policy — a plain <c>admin</c> is
/// authorized for user CRUD and lockouts (via <c>admin_panel:*</c> page policies) but is rejected
/// by ASP.NET Core's authorization middleware before this controller's action body ever runs for
/// the super-admin-only endpoints below, closing SEC-01. Every endpoint that accepts a
/// client-supplied resource id additionally goes through the SEC-16 resource-level authorization
/// check in its handler (<c>IResourceAuthorizationService</c>) — role-level policy alone is never
/// treated as sufficient proof the caller may act on that specific row.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
public sealed class AdminController : ControllerBase
{
    private const int DefaultPageSize = 25;
    private const string SuperAdminClaimType = "is_super_admin";
    private const string DefaultExportFormat = "json";

    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="AdminController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch admin commands/queries.</param>
    public AdminController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lists users, paginated and optionally filtered by a search term.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="search">Optional case-insensitive substring match against email/name.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated user list.</returns>
    [HttpGet("users")]
    [Authorize(Policy = "admin_panel:view")]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> ListUsersAsync(
        [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        PagedQuery pagedQuery = BuildPagedQuery(page, pageSize);
        Result<PagedResult<UserSummaryDto>> result = await _sender.Send(new ListUsersQuery(pagedQuery, search), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Creates a new user account.</summary>
    /// <param name="request">The new user's profile, role, and optional initial password.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the new user and one-time temporary password (if generated), or 400 on validation/escalation failure.</returns>
    [HttpPost("users")]
    [Authorize(Policy = "admin_panel:create")]
    public async Task<ActionResult> CreateUserAsync([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        (var actorUserId, var actorIsSuperAdmin) = ReadActor();
        var command = new CreateUserCommand(actorUserId, actorIsSuperAdmin, request.Email, request.Name, request.Title, request.Department, request.RoleId, request.Password);
        Result<CreateUserResult> result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new { user = result.Value!.User, tempPassword = result.Value.TemporaryPassword })
            : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Fetches a single user's admin-facing detail.</summary>
    /// <param name="userId">The user id being looked up.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the user detail, or 404 if not found/not reachable by this actor.</returns>
    [HttpGet("users/{userId:int}")]
    [Authorize(Policy = "admin_panel:view")]
    public async Task<ActionResult<UserDetailDto>> GetUserAsync(int userId, CancellationToken cancellationToken)
    {
        (var actorUserId, var actorIsSuperAdmin) = ReadActor();
        Result<UserDetailDto> result = await _sender.Send(new GetUserByIdQuery(actorUserId, actorIsSuperAdmin, userId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Updates a user's profile fields. Does not change roles — see <see cref="AssignRoleAsync"/>.</summary>
    /// <param name="userId">The user being updated.</param>
    /// <param name="request">The profile fields to change.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the updated detail, or 400/404 on failure.</returns>
    [HttpPatch("users/{userId:int}")]
    [Authorize(Policy = "admin_panel:edit")]
    public async Task<ActionResult<UserDetailDto>> UpdateUserAsync(int userId, [FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        (var actorUserId, var actorIsSuperAdmin) = ReadActor();
        var command = new UpdateUserProfileCommand(actorUserId, actorIsSuperAdmin, userId, request.Name, request.Title, request.Department, request.Email);
        Result<UserDetailDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Soft-deletes a user account. A user may never delete themselves.</summary>
    /// <param name="userId">The user being deleted.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 400 on failure (self-delete, not found, forbidden peer).</returns>
    [HttpDelete("users/{userId:int}")]
    [Authorize(Policy = "admin_panel:delete")]
    public async Task<ActionResult> DeleteUserAsync(int userId, CancellationToken cancellationToken)
    {
        (var actorUserId, var actorIsSuperAdmin) = ReadActor();
        Result<bool> result = await _sender.Send(new DeleteUserCommand(actorUserId, actorIsSuperAdmin, userId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Toggles a user's active/suspended state. A user may never suspend themselves.</summary>
    /// <param name="userId">The user whose active state is toggled.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the resulting <c>isActive</c> flag, or 400 on failure.</returns>
    [HttpPost("users/{userId:int}/suspend")]
    [Authorize(Policy = "admin_panel:edit")]
    public async Task<ActionResult> ToggleSuspensionAsync(int userId, CancellationToken cancellationToken)
    {
        (var actorUserId, var actorIsSuperAdmin) = ReadActor();
        Result<bool> result = await _sender.Send(new SetUserSuspensionCommand(actorUserId, actorIsSuperAdmin, userId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true, isActive = result.Value }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Forces a password reset, returning a one-time temporary password.</summary>
    /// <param name="userId">The user whose password is reset.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the temporary password, or 400 on failure.</returns>
    [HttpPost("users/{userId:int}/reset-password")]
    [Authorize(Policy = "admin_panel:edit")]
    public async Task<ActionResult> ResetPasswordAsync(int userId, CancellationToken cancellationToken)
    {
        (var actorUserId, var actorIsSuperAdmin) = ReadActor();
        Result<string> result = await _sender.Send(new ResetUserPasswordCommand(actorUserId, actorIsSuperAdmin, userId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true, tempPassword = result.Value }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Assigns a role to a user. Restricted to super-admin callers — this is the endpoint
    /// (closing SEC-01) that a plain admin must never be able to reach, because it is the only
    /// path that can grant <c>super_admin</c> itself.
    /// </summary>
    /// <param name="userId">The user to assign the role to.</param>
    /// <param name="request">The role to assign.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, 403 if the caller isn't a super-admin, 400 for an invalid escalation attempt.</returns>
    [HttpPost("users/{userId:int}/roles")]
    [Authorize(Policy = AuthorizationPolicyExtensions.SuperAdminPolicyName)]
    public async Task<ActionResult> AssignRoleAsync(int userId, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        (var actorUserId, var actorIsSuperAdmin) = ReadActor();
        var command = new AssignUserRoleCommand(actorUserId, actorIsSuperAdmin, userId, request.RoleId);
        Result<bool> result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(new { ok = true })
            : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Clears a user's lockout and resets their failed-attempt counter.</summary>
    /// <param name="userId">The locked user's id.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 400 if the user doesn't exist.</returns>
    [HttpPost("users/{userId:int}/unlock")]
    [Authorize(Policy = "admin_panel:edit")]
    public async Task<ActionResult> UnlockUserAsync(int userId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<bool> result = await _sender.Send(new UnlockUserCommand(actorUserId, userId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Lists every role with its user count.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated role list.</returns>
    [HttpGet("roles")]
    [Authorize(Policy = "admin_panel:view")]
    public async Task<ActionResult<PagedResult<RoleSummaryDto>>> ListRolesAsync([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        Result<PagedResult<RoleSummaryDto>> result = await _sender.Send(new ListRolesQuery(BuildPagedQuery(page, pageSize)), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Creates a custom (non-system) role. Restricted to super-admin callers.</summary>
    /// <param name="request">The new role's name/label/description.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the new role, or 400 on failure.</returns>
    [HttpPost("roles")]
    [Authorize(Policy = AuthorizationPolicyExtensions.SuperAdminPolicyName)]
    public async Task<ActionResult> CreateRoleAsync([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        (var actorUserId, var _) = ReadActor();
        Result<RoleSummaryDto> result = await _sender.Send(new CreateRoleCommand(actorUserId, request.Name, request.NameAr, request.Description), cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new { role = result.Value })
            : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Edits a role's display fields. Restricted to super-admin callers.</summary>
    /// <param name="roleId">The role being edited.</param>
    /// <param name="request">The display fields to change.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 400 if the role doesn't exist.</returns>
    [HttpPatch("roles/{roleId:int}")]
    [Authorize(Policy = AuthorizationPolicyExtensions.SuperAdminPolicyName)]
    public async Task<ActionResult> UpdateRoleAsync(int roleId, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        (var actorUserId, var _) = ReadActor();
        Result<bool> result = await _sender.Send(new UpdateRoleCommand(actorUserId, roleId, request.NameAr, request.Description), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Soft-deletes a custom role. Restricted to super-admin callers. Blocked for system roles or roles with assigned users.</summary>
    /// <param name="roleId">The role being deleted.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 400 on failure.</returns>
    [HttpDelete("roles/{roleId:int}")]
    [Authorize(Policy = AuthorizationPolicyExtensions.SuperAdminPolicyName)]
    public async Task<ActionResult> DeleteRoleAsync(int roleId, CancellationToken cancellationToken)
    {
        (var actorUserId, var _) = ReadActor();
        Result<bool> result = await _sender.Send(new DeleteRoleCommand(actorUserId, roleId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Fetches a single role's page × permission grant matrix.</summary>
    /// <param name="roleId">The role being read.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the matrix, or 400 if the role doesn't exist.</returns>
    [HttpGet("roles/{roleId:int}/permissions")]
    [Authorize(Policy = "admin_panel:view")]
    public async Task<ActionResult<RolePermissionMatrixDto>> GetRolePermissionsAsync(int roleId, CancellationToken cancellationToken)
    {
        Result<RolePermissionMatrixDto> result = await _sender.Send(new GetRolePermissionMatrixQuery(roleId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Replaces a role's page × permission matrix. Restricted to super-admin callers (task
    /// requirement 4: a plain admin must not be able to change role permissions).
    /// </summary>
    /// <param name="roleId">The role whose grants are being replaced.</param>
    /// <param name="request">The full replacement grant set.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 400 if the role doesn't exist.</returns>
    [HttpPut("roles/{roleId:int}/permissions")]
    [Authorize(Policy = AuthorizationPolicyExtensions.SuperAdminPolicyName)]
    public async Task<ActionResult> UpdateRolePermissionsAsync(int roleId, [FromBody] UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        (var actorUserId, var _) = ReadActor();
        var grants = request.Permissions
            .SelectMany(pageEntry => pageEntry.Value.Select(verb => (pageEntry.Key, verb)))
            .ToList();

        Result<bool> result = await _sender.Send(new UpdateRolePermissionsCommand(actorUserId, roleId, grants), cancellationToken);
        return result.IsSuccess
            ? Ok(new { ok = true })
            : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Fetches the full effective permission matrix (every user × every page), paginated by user.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the matrix.</returns>
    [HttpGet("matrix")]
    [Authorize(Policy = "admin_panel:view")]
    public async Task<ActionResult<EffectivePermissionMatrixDto>> GetMatrixAsync([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        Result<EffectivePermissionMatrixDto> result = await _sender.Send(new GetEffectivePermissionMatrixQuery(BuildPagedQuery(page, pageSize)), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Sets or clears a per-user page/permission override. Restricted to super-admin callers.</summary>
    /// <param name="request">The override target and grant kind (or <c>null</c> to clear).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 400 on failure.</returns>
    [HttpPut("matrix/user-override")]
    [Authorize(Policy = AuthorizationPolicyExtensions.SuperAdminPolicyName)]
    public async Task<ActionResult> SetUserOverrideAsync([FromBody] SetUserPermissionOverrideRequest request, CancellationToken cancellationToken)
    {
        (var actorUserId, var _) = ReadActor();
        var command = new SetUserPermissionOverrideCommand(actorUserId, request.UserId, request.PageSlug, request.PermName, request.GrantType);
        Result<bool> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Exports the full permission matrix as CSV or JSON. A purpose-built, uncapped export
    /// endpoint per DOTNET-CONVENTIONS.md §8's R-BE-033-vs-exports interpretation — still
    /// restricted to super-admin callers.
    /// </summary>
    /// <param name="format">Either <c>csv</c> or <c>json</c> (defaults to <c>json</c>).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the rendered file.</returns>
    [HttpGet("matrix/export")]
    [Authorize(Policy = AuthorizationPolicyExtensions.SuperAdminPolicyName)]
    public async Task<ActionResult> ExportMatrixAsync([FromQuery] string? format, CancellationToken cancellationToken)
    {
        Result<PermissionMatrixExportDto> result = await _sender.Send(new ExportPermissionMatrixQuery(format ?? DefaultExportFormat), cancellationToken);
        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>Queries the paginated activity/audit log.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="userId">Optional filter to a single acting user.</param>
    /// <param name="action">Optional filter to an exact action name.</param>
    /// <param name="dateFrom">Optional inclusive lower bound (UTC).</param>
    /// <param name="dateTo">Optional inclusive upper bound (UTC).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated activity log.</returns>
    [HttpGet("activity")]
    [Authorize(Policy = "admin_panel:view")]
    public async Task<ActionResult<PagedResult<ActivityLogEntryDto>>> ListActivityAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] int? userId,
        [FromQuery] string? action,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var query = new ListActivityLogQuery(BuildPagedQuery(page, pageSize), userId, action, dateFrom, dateTo);
        Result<PagedResult<ActivityLogEntryDto>> result = await _sender.Send(query, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Exports the activity/audit log as a UTF-8-with-BOM CSV, streamed directly to the response
    /// body (task requirement: never buffer the whole log in memory). Ports the old Node
    /// <c>GET /admin/activity/export</c> (<c>admin.ts:637</c>) that was missed during the port —
    /// the frontend's export button called this path and got a 404 until now. Same authorization
    /// policy as <see cref="ListActivityAsync"/> (the JSON list sibling) since this is the same
    /// data, just a different rendering — and exporting the full log is itself security-relevant,
    /// so the handler writes a dedicated audit-log entry on every successful export.
    /// </summary>
    /// <param name="userId">Optional filter to a single acting user.</param>
    /// <param name="action">Optional filter to an exact action name.</param>
    /// <param name="dateFrom">Optional inclusive lower bound (UTC).</param>
    /// <param name="dateTo">Optional inclusive upper bound (UTC).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 with a streamed <c>text/csv</c> body, capped at <see cref="ExportActivityLogQueryHandler.MaxRows"/> rows.</returns>
    [HttpGet("activity/export")]
    [Authorize(Policy = "admin_panel:view")]
    public async Task<IActionResult> ExportActivityAsync(
        [FromQuery] int? userId,
        [FromQuery] string? action,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        (var actorUserId, var _) = ReadActor();
        var query = new ExportActivityLogQuery(actorUserId, userId, action, dateFrom, dateTo);
        Result<ActivityLogExportDto> result = await _sender.Send(query, cancellationToken);

        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = "attachment; filename=\"activity-log.csv\"";
        await ActivityLogCsvWriter.WriteAsync(result.Value!.Rows, Response.Body, cancellationToken);
        return new EmptyResult();
    }

    /// <summary>
    /// Reads system settings (password policy, session duration, Azure AD config). Secret keys
    /// are always masked in the response — this port has no unmasked read path at all.
    /// </summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the settings map.</returns>
    [HttpGet("settings")]
    [Authorize(Policy = "settings:view")]
    public async Task<ActionResult> GetSettingsAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyDictionary<string, string>> result = await _sender.Send(new GetSystemSettingsQuery(), cancellationToken);
        return Ok(new { settings = result.Value });
    }

    /// <summary>Updates system settings. Every key is whitelist-validated before any write.</summary>
    /// <param name="request">The key/value pairs to upsert.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 400 if any key is not whitelisted.</returns>
    [HttpPut("settings")]
    [Authorize(Policy = AuthorizationPolicyExtensions.SuperAdminPolicyName)]
    public async Task<ActionResult> UpdateSettingsAsync([FromBody] UpdateSystemSettingsRequest request, CancellationToken cancellationToken)
    {
        (var actorUserId, var _) = ReadActor();
        Result<bool> result = await _sender.Send(new UpdateSystemSettingsCommand(actorUserId, request.Settings), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    private static PagedQuery BuildPagedQuery(int page, int pageSize) =>
        new() { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? DefaultPageSize : pageSize };

    private (int ActorUserId, bool ActorIsSuperAdmin) ReadActor()
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var actorIsSuperAdmin = User.FindFirst(SuperAdminClaimType)?.Value == bool.TrueString;
        return (actorUserId, actorIsSuperAdmin);
    }
}
