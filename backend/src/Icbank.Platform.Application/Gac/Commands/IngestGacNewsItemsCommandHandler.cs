using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Gac.News;
using Icbank.Platform.Domain.Gac;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>Handles <see cref="IngestGacNewsItemsCommand"/>.</summary>
public sealed class IngestGacNewsItemsCommandHandler
    : IRequestHandler<IngestGacNewsItemsCommand, Result<IngestGacNewsItemsResult>>
{
    /// <summary>
    /// Mirrors the <c>source_url</c> column width. Anything longer is skipped rather than
    /// handed to the database, which would otherwise fail the whole batch with a 500.
    /// </summary>
    private const int SourceUrlMaxLength = 2048;

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
        Dictionary<string, GacNewsItem> byUrl = await LoadExistingByUrlAsync(request.Items, cancellationToken);

        var inserted = 0;
        var updated = 0;
        var skipped = 0;
        var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (IngestGacNewsItem item in request.Items)
        {
            var url = item.SourceUrl.Trim();
            if (url.Length > SourceUrlMaxLength || !seenInBatch.Add(url))
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
        var incomingBody = NewsBodySanitizer.Sanitize(item.TitleAr, item.BodyAr, item.SourceName);
        if (incomingBody is not null)
        {
            existing.BodyAr = incomingBody;
        }
        else if (NewsBodySanitizer.Sanitize(existing.TitleAr, existing.BodyAr, existing.ExternalRef) is null)
        {
            // The stored body only restates its own headline, so clearing it loses nothing. This
            // is what retires the echoed summaries written before this rule existed.
            existing.BodyAr = null;
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
        BodyAr = NewsBodySanitizer.Sanitize(item.TitleAr, item.BodyAr, item.SourceName),
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

    /// <summary>
    /// Loads the already-stored rows for the batch's URLs in a single query.
    /// </summary>
    /// <param name="items">The submitted batch.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The existing rows, keyed by source URL.</returns>
    /// <remarks>
    /// One query for the whole batch rather than one per item: a weekly fetch across four search
    /// terms submits up to 200 rows, and a per-row lookup would both round-trip 200 times and throw
    /// if the table already held two rows for a URL. There is no unique index on <c>source_url</c>,
    /// so duplicates are possible for anything ingested before this handler existed; the first row
    /// wins rather than the call failing.
    /// </remarks>
    private async Task<Dictionary<string, GacNewsItem>> LoadExistingByUrlAsync(
        IReadOnlyList<IngestGacNewsItem> items, CancellationToken cancellationToken)
    {
        var urls = items.Select(i => i.SourceUrl.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        List<GacNewsItem> existingItems = await _queryExecutor.ToListAsync(
            _dbContext.GacNewsItems.Where(n => n.SourceUrl != null && urls.Contains(n.SourceUrl)), cancellationToken);

        var byUrl = new Dictionary<string, GacNewsItem>(StringComparer.OrdinalIgnoreCase);
        foreach (GacNewsItem row in existingItems.Where(r => r.SourceUrl is not null))
        {
            byUrl.TryAdd(row.SourceUrl!, row);
        }

        return byUrl;
    }
}
