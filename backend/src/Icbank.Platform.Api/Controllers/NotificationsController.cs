using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Application.Shorfah.Commands;
using Icbank.Platform.Application.Shorfah.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports the Shorfah in-app notification inbox (API-SURFACE.md §19 -- not namespaced under
/// <c>/shorfah</c> in the Node source despite living in the same file, preserved here). This is
/// per-user data and the primary IDOR surface the task brief names explicitly: every route runs
/// <see cref="IResourceAuthorizationService.AuthorizeShorfahNotificationResourceAsync"/>, which
/// checks existence AND ownership in the same query so a foreign notification id is
/// indistinguishable from a nonexistent one (SEC-16).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private const int DefaultPageSize = 30;

    private readonly ISender _sender;
    private readonly IResourceAuthorizationService _resourceAuthorization;

    /// <summary>Initializes a new instance of the <see cref="NotificationsController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch notification commands/queries.</param>
    /// <param name="resourceAuthorization">The SEC-16 resource-existence-and-ownership port.</param>
    public NotificationsController(ISender sender, IResourceAuthorizationService resourceAuthorization)
    {
        _sender = sender;
        _resourceAuthorization = resourceAuthorization;
    }

    /// <summary>Lists the caller's own notifications, newest first, paginated. Node source capped at latest 30; this port exposes real pagination.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size; defaults to 30 to match the Node source's fixed limit.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated notification envelope.</returns>
    [HttpGet]
    [Authorize(Policy = "shorfah:view")]
    public async Task<ActionResult> ListAsync([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var actorUserId = RequireActorUserId();
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? DefaultPageSize : pageSize };
        Result<PagedResult<ShorfahNotificationDto>> result = await _sender.Send(new ListShorfahNotificationsQuery(actorUserId, pagedQuery), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Marks one notification read. Scoped to the caller's own id -- a foreign notification id resolves to 404, never a silent no-op or another user's data.</summary>
    /// <param name="notificationId">The notification being marked read.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{ok:true}</c>, or 404.</returns>
    [HttpPost("{notificationId:int}/read")]
    [Authorize(Policy = "shorfah:view")]
    public async Task<ActionResult> MarkReadAsync(int notificationId, CancellationToken cancellationToken)
    {
        var actorUserId = RequireActorUserId();
        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeShorfahNotificationResourceAsync(actorUserId, notificationId, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return NotFound(new { error = "الإشعار غير موجود" });
        }

        Result<bool> result = await _sender.Send(new MarkShorfahNotificationReadCommand(actorUserId, notificationId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : NotFound(new { error = result.Error });
    }

    /// <summary>Marks all of the caller's own notifications read.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{ok:true}</c>.</returns>
    [HttpPost("read-all")]
    [Authorize(Policy = "shorfah:view")]
    public async Task<ActionResult> MarkAllReadAsync(CancellationToken cancellationToken)
    {
        var actorUserId = RequireActorUserId();
        await _sender.Send(new MarkAllShorfahNotificationsReadCommand(actorUserId), cancellationToken);
        return Ok(new { ok = true });
    }

    private int RequireActorUserId() =>
        CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
}
