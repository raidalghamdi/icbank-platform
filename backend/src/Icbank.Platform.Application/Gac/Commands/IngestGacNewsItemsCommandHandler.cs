using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Gac;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>Handles <see cref="IngestGacNewsItemsCommand"/>.</summary>
public sealed class IngestGacNewsItemsCommandHandler
    : IRequestHandler<IngestGacNewsItemsCommand, Result<IngestGacNewsItemsResult>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="IngestGacNewsItemsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public IngestGacNewsItemsCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<IngestGacNewsItemsResult>> Handle(
        IngestGacNewsItemsCommand request, CancellationToken cancellationToken)
    {
        var inserted = 0;
        var updated = 0;
        var skipped = 0;
        var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var urls = request.Items.Select(i => i.SourceUrl.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // One query for the whole batch rather than one per item: a weekly fetch across four search
        // terms submits up to 200 rows, and SingleOrDefaultAsync per row would both round-trip 200
        // times and throw if the table already held two rows for a URL (there is no unique index on
        // source_url, so that is possible for anything ingested before this handler existed).
        List<GacNewsItem> existingItems = await _queryExecutor.ToListAsync(
            _dbContext.GacNewsItems.Where(n => n.SourceUrl != null && urls.Contains(n.SourceUrl)), cancellationToken);

        var byUrl = new Dictionary<string, GacNewsItem>(StringComparer.OrdinalIgnoreCase);
        foreach (GacNewsItem row in existingItems.Where(r => r.SourceUrl is not null))
        {
            byUrl.TryAdd(row.SourceUrl!, row);
        }

        foreach (IngestGacNewsItem item in request.Items)
        {
            var url = item.SourceUrl.Trim();
            if (!seenInBatch.Add(url))
            {
                skipped++;
                continue;
            }

            if (byUrl.TryGetValue(url, out GacNewsItem? existing))
            {
                ApplyUpdate(existing, item);
                updated++;
                continue;
            }

            _dbContext.Add(ToNewEntity(item, url));
            inserted++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<IngestGacNewsItemsResult>.Success(new IngestGacNewsItemsResult(inserted, updated, skipped));
    }

    /// <summary>
    /// Refreshes a previously ingested article in place.
    /// </summary>
    /// <param name="existing">The row already stored for this URL.</param>
    /// <param name="item">The freshly fetched version.</param>
    /// <remarks>
    /// A body is only ever added, never removed. Google News supplies headline-only items while a
    /// licensed provider may supply full prose for the same URL, so letting a later headline-only
    /// fetch null out an existing body would silently degrade the corpus the report generator reads.
    /// </remarks>
    private static void ApplyUpdate(GacNewsItem existing, IngestGacNewsItem item)
    {
        existing.TitleAr = item.TitleAr.Trim();
        if (!string.IsNullOrWhiteSpace(item.BodyAr))
        {
            existing.BodyAr = item.BodyAr.Trim();
        }

        existing.PublishedAt = item.PublishedAt ?? existing.PublishedAt;
        existing.ExternalRef = string.IsNullOrWhiteSpace(item.SourceName) ? existing.ExternalRef : item.SourceName.Trim();

        if (ParseCategory(item.Category) is GacNewsCategory category)
        {
            existing.Category = category;
        }

        if (item.Tags is { Count: > 0 })
        {
            existing.Tags = item.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct().ToList();
        }
    }

    private static GacNewsItem ToNewEntity(IngestGacNewsItem item, string url) => new()
    {
        Kind = ParseKind(item.Kind),
        TitleAr = item.TitleAr.Trim(),
        BodyAr = string.IsNullOrWhiteSpace(item.BodyAr) ? null : item.BodyAr.Trim(),
        Category = ParseCategory(item.Category),
        SourceUrl = url,
        PublishedAt = item.PublishedAt,
        ExternalRef = string.IsNullOrWhiteSpace(item.SourceName) ? null : item.SourceName.Trim(),
        Tags = item.Tags is null
            ? new List<string>()
            : item.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct().ToList(),
    };

    private static GacNewsKind ParseKind(string? value) =>
        Enum.TryParse<GacNewsKind>(value, ignoreCase: true, out GacNewsKind parsed) ? parsed : GacNewsKind.News;

    private static GacNewsCategory? ParseCategory(string? value) =>
        Enum.TryParse<GacNewsCategory>(value, ignoreCase: true, out GacNewsCategory parsed) ? parsed : null;
}
