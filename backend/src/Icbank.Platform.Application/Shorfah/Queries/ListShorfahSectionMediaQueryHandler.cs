using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>
/// Handles <see cref="ListShorfahSectionMediaQuery"/>. Ports <c>shorfah.ts:548-556</c>, and closes
/// the read-side half of AMBIGUOUS-API-4: an actor must hold at least the <c>View</c> tier (or
/// Contribute/Review/Approve, or be admin) on the owning section, matching the tier check the
/// upload/patch/delete mutations already enforce -- a section with no permission rows granted to
/// the caller must not leak even its media list.
/// </summary>
public sealed class ListShorfahSectionMediaQueryHandler : IRequestHandler<ListShorfahSectionMediaQuery, Result<PagedResult<ShorfahSectionMediaDto>>>
{
    /// <summary>The sentinel error returned when the caller lacks any qualifying tier on the section.</summary>
    public const string ForbiddenError = "غير مصرح";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IShorfahSectionAccessService _accessService;

    /// <summary>Initializes a new instance of the <see cref="ListShorfahSectionMediaQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="accessService">The per-section permission-tier port.</param>
    public ListShorfahSectionMediaQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IShorfahSectionAccessService accessService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _accessService = accessService;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ShorfahSectionMediaDto>>> Handle(ListShorfahSectionMediaQuery request, CancellationToken cancellationToken)
    {
        var allowed = await IsAllowedAsync(request.ActorUserId, request.SectionId, cancellationToken);
        if (!allowed)
        {
            return Result<PagedResult<ShorfahSectionMediaDto>>.Failure(ForbiddenError);
        }

        IQueryable<ShorfahSectionMedia> ordered = _dbContext.ShorfahSectionMedia
            .Where(m => m.SectionId == request.SectionId)
            .OrderBy(m => m.DisplayOrder);

        List<int> allIds = await _queryExecutor.ToListAsync(ordered.Select(m => m.Id), cancellationToken);

        List<ShorfahSectionMedia> page = await _queryExecutor.ToListAsync(
            ordered.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);

        var items = page
            .Select(m => new ShorfahSectionMediaDto(m.Id, m.SectionId, m.MediaUrl, m.MediaType.ToString(), m.CaptionAr, m.DisplayOrder))
            .ToList();

        return Result<PagedResult<ShorfahSectionMediaDto>>.Success(
            new PagedResult<ShorfahSectionMediaDto>(items, request.Query.Page, request.Query.PageSize, allIds.Count));
    }

    private async Task<bool> IsAllowedAsync(int actorUserId, int sectionId, CancellationToken cancellationToken)
    {
        if (await _accessService.IsAdminAsync(actorUserId, cancellationToken))
        {
            return true;
        }

        return await _accessService.CanAccessSectionAsync(actorUserId, sectionId, ShorfahSectionAccessTier.View, cancellationToken)
            || await _accessService.CanAccessSectionAsync(actorUserId, sectionId, ShorfahSectionAccessTier.Contribute, cancellationToken)
            || await _accessService.CanAccessSectionAsync(actorUserId, sectionId, ShorfahSectionAccessTier.Review, cancellationToken)
            || await _accessService.CanAccessSectionAsync(actorUserId, sectionId, ShorfahSectionAccessTier.Approve, cancellationToken);
    }
}
