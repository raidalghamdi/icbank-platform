using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Api.Extensions;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Gac;
using Icbank.Platform.Application.Gac.Commands;
using Icbank.Platform.Application.Gac.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports <c>routes/gac.ts</c> (API-SURFACE.md §12): the GAC publications library, cached social
/// feed, and news feed. The Node source left the four GET routes unauthenticated by mount-order
/// accident; every route here requires <c>[Authorize]</c> at minimum (closes SEC-02 for this
/// route family — task requirement: "NO anonymous mutating endpoints", extended here to reads
/// too since no page in the RBAC catalogue is dedicated to GAC content and a blanket
/// authenticated-read posture is the safer default). Mutating routes require <c>super-admin</c>
/// (no dedicated GAC page slug exists in the seeded RBAC catalogue — flagged for product
/// sign-off in WAVE2-PORT-NOTES.md) except the cron-driven ingest route, which uses the shared
/// <c>cron-api-key</c> policy established in Wave 1 rather than introducing a second parallel
/// shared-secret mechanism.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gac")]
public sealed class GacController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="GacController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch GAC content commands/queries.</param>
    public GacController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lists published publications with optional search/category/language filters.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="q">Optional fuzzy search text.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="language">Optional language filter.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated publication list.</returns>
    [HttpGet("publications")]
    [Authorize]
    public async Task<ActionResult<PagedResult<GacPublicationDto>>> ListPublicationsAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] string? language,
        CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<GacPublicationDto>> result =
            await _sender.Send(new ListGacPublicationsQuery(pagedQuery, q, category, language), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Returns category counts for the publications filter chips.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the category counts.</returns>
    [HttpGet("publications/categories")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<GacPublicationCategoryCountDto>>> ListPublicationCategoriesAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<GacPublicationCategoryCountDto>> result =
            await _sender.Send(new ListGacPublicationCategoriesQuery(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Idempotently reseeds publication metadata rows.</summary>
    /// <param name="request">The publication metadata rows to reseed.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the reseed summary.</returns>
    [HttpPost("publications/reseed")]
    [Authorize(Policy = AuthorizationPolicyExtensions.SuperAdminPolicyName)]
    public async Task<ActionResult<ReseedGacPublicationsResult>> ReseedPublicationsAsync(
        [FromBody] ReseedGacPublicationsRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<ReseedGacPublicationsResult> result =
            await _sender.Send(new ReseedGacPublicationsCommand(actorUserId, request.Publications), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Upserts a batch of social posts (called by an hourly external cron).</summary>
    /// <param name="request">The batch of posts to upsert.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the ingest summary, or 400 on validation failure.</returns>
    [HttpPost("social-feed/ingest")]
    [Authorize(Policy = AuthorizationPolicyExtensions.CronApiKeyPolicyName)]
    public async Task<ActionResult<IngestGacSocialPostsResult>> IngestSocialPostsAsync(
        [FromBody] IngestGacSocialPostsRequest request, CancellationToken cancellationToken)
    {
        Result<IngestGacSocialPostsResult> result = await _sender.Send(new IngestGacSocialPostsCommand(request.Posts), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Upserts a batch of news items, deduplicated by source URL.</summary>
    /// <param name="request">The batch of news items to upsert.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the ingest summary, or 400 on validation failure.</returns>
    [HttpPost("news/ingest")]
    [Authorize(Policy = AuthorizationPolicyExtensions.CronApiKeyPolicyName)]
    public async Task<ActionResult<IngestGacNewsItemsResult>> IngestNewsAsync(
        [FromBody] IngestGacNewsRequest request, CancellationToken cancellationToken)
    {
        Result<IngestGacNewsItemsResult> result =
            await _sender.Send(new IngestGacNewsItemsCommand(request.Items), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Pulls fresh coverage from the enabled news providers and ingests it.</summary>
    /// <param name="request">Optional per-run overrides for the search terms and lookback window.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the fetch summary, including per-provider yields.</returns>
    /// <remarks>
    /// Returns 200 with zero counts rather than an error when a provider yields nothing, because the
    /// upstream is a best-effort feed and an empty week is a legitimate outcome, not a failure the
    /// cron should retry.
    /// </remarks>
    [HttpPost("news/fetch")]
    [Authorize(Policy = AuthorizationPolicyExtensions.CronApiKeyPolicyName)]
    public async Task<ActionResult<FetchGacNewsResult>> FetchNewsAsync(
        [FromBody] FetchGacNewsRequest? request, CancellationToken cancellationToken)
    {
        Result<FetchGacNewsResult> result =
            await _sender.Send(new FetchGacNewsCommand(request?.Terms, request?.WithinDays), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Seeds 5 fixed sample Twitter/X posts (fixture data pending real X API integration).</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the seed summary.</returns>
    [HttpPost("social-feed/seed-twitter")]
    [Authorize(Policy = AuthorizationPolicyExtensions.SuperAdminPolicyName)]
    public async Task<ActionResult<SeedGacTwitterSamplesResult>> SeedTwitterSamplesAsync(CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<SeedGacTwitterSamplesResult> result = await _sender.Send(new SeedGacTwitterSamplesCommand(actorUserId), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Lists the latest cached social posts.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="platform">Optional platform filter.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated social post list.</returns>
    [HttpGet("social-feed")]
    [Authorize]
    public async Task<ActionResult<PagedResult<GacSocialPostDto>>> ListSocialPostsAsync(
        [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? platform, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<GacSocialPostDto>> result = await _sender.Send(new ListGacSocialPostsQuery(pagedQuery, platform), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Lists the latest news/decisions feed items.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="kind">Optional item-kind filter.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated news item list.</returns>
    [HttpGet("news")]
    [Authorize]
    public async Task<ActionResult<PagedResult<GacNewsItemDto>>> ListNewsItemsAsync(
        [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? kind, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<GacNewsItemDto>> result = await _sender.Send(new ListGacNewsItemsQuery(pagedQuery, kind), cancellationToken);
        return Ok(result.Value);
    }
}
