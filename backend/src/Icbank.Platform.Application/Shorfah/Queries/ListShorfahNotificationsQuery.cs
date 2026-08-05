using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>
/// Query for <c>GET /notifications</c> (API-SURFACE.md §19). Ports <c>shorfah.ts:1000-1009</c>:
/// strictly scoped to the caller's own id (the primary IDOR surface named by the task brief) --
/// there is no <c>userId</c> parameter anywhere in this query by design.
/// </summary>
/// <param name="UserId">The authenticated caller's id.</param>
/// <param name="Query">The pagination parameters.</param>
public sealed record ListShorfahNotificationsQuery(int UserId, PagedQuery Query) : IRequest<Result<PagedResult<ShorfahNotificationDto>>>;
