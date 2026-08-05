using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Gac;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>Handles <see cref="IngestGacSocialPostsCommand"/>.</summary>
public sealed class IngestGacSocialPostsCommandHandler : IRequestHandler<IngestGacSocialPostsCommand, Result<IngestGacSocialPostsResult>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="IngestGacSocialPostsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public IngestGacSocialPostsCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<IngestGacSocialPostsResult>> Handle(IngestGacSocialPostsCommand request, CancellationToken cancellationToken)
    {
        var inserted = 0;
        var updated = 0;

        foreach (IngestGacSocialPostItem item in request.Posts)
        {
            var wasUpdate = await UpsertOneAsync(item, cancellationToken);
            if (wasUpdate)
            {
                updated++;
                continue;
            }

            inserted++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<IngestGacSocialPostsResult>.Success(new IngestGacSocialPostsResult(inserted, updated));
    }

    private static void ApplyUpdate(GacSocialPost existing, IngestGacSocialPostItem item, GacSocialMediaType mediaType)
    {
        existing.ContentAr = item.ContentAr;
        existing.ContentEn = item.ContentEn;
        existing.PostUrl = item.PostUrl ?? existing.PostUrl;
        existing.MediaUrl = item.MediaUrl;
        existing.MediaType = mediaType;
        existing.PostedAt = item.PostedAt;
        existing.Account = item.Account ?? existing.Account;
    }

    private static GacSocialPost ToNewEntity(IngestGacSocialPostItem item, GacSocialPlatform platform, GacSocialMediaType mediaType) => new()
    {
        Platform = platform,
        ExternalId = item.ExternalId,
        ContentAr = item.ContentAr,
        ContentEn = item.ContentEn,
        PostUrl = item.PostUrl ?? string.Empty,
        MediaUrl = item.MediaUrl,
        MediaType = mediaType,
        PostedAt = item.PostedAt,
        Account = item.Account ?? string.Empty,
    };

    private async Task<bool> UpsertOneAsync(IngestGacSocialPostItem item, CancellationToken cancellationToken)
    {
        GacSocialPlatform platform = Enum.Parse<GacSocialPlatform>(item.Platform, ignoreCase: true);
        GacSocialMediaType mediaType = Enum.TryParse(item.MediaType, ignoreCase: true, out GacSocialMediaType parsedMediaType)
            ? parsedMediaType
            : GacSocialMediaType.None;

        GacSocialPost? existing = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.GacSocialPosts.Where(p => p.Platform == platform && p.ExternalId == item.ExternalId), cancellationToken);

        if (existing is not null)
        {
            ApplyUpdate(existing, item, mediaType);
            return true;
        }

        _dbContext.Add(ToNewEntity(item, platform, mediaType));
        return false;
    }
}
