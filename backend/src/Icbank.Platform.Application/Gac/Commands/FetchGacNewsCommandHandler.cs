using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Gac.News;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>Handles <see cref="FetchGacNewsCommand"/>.</summary>
public sealed class FetchGacNewsCommandHandler : IRequestHandler<FetchGacNewsCommand, Result<FetchGacNewsResult>>
{
    private readonly IReadOnlyList<INewsSourceProvider> _providers;
    private readonly NewsFetchSettings _settings;
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="FetchGacNewsCommandHandler"/> class.</summary>
    /// <param name="providers">The enabled news providers, in configured order.</param>
    /// <param name="settings">The search settings.</param>
    /// <param name="sender">The mediator used to delegate persistence to the ingest command.</param>
    public FetchGacNewsCommandHandler(
        IEnumerable<INewsSourceProvider> providers,
        NewsFetchSettings settings,
        ISender sender)
    {
        _providers = providers.ToList();
        _settings = settings;
        _sender = sender;
    }

    /// <inheritdoc />
    public async Task<Result<FetchGacNewsResult>> Handle(FetchGacNewsCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> terms = request.Terms is { Count: > 0 } ? request.Terms : _settings.Terms;
        var withinDays = request.WithinDays is > 0 ? request.WithinDays.Value : _settings.WithinDays;

        var perProvider = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        List<FetchedNewsItem> collected = await CollectAsync(terms, withinDays, perProvider, cancellationToken);

        // Deduplicate before handing off so the ingest command's Skipped count reports genuine
        // cross-term overlap rather than noise: the four configured terms overlap heavily, and the
        // same article routinely matches three of them. The richest body wins.
        var batch = collected
            .GroupBy(i => i.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .Select(g => ToIngestItem(g.OrderByDescending(i => i.Body?.Length ?? 0).First()))
            .ToList();

        // A zero-item run is reported as a success with empty counts, not an error. The Application
        // layer has no logger by design, so PerProvider is the diagnostic surface: it distinguishes
        // "no news this week" from "one provider is silently returning nothing".
        if (batch.Count == 0)
        {
            return Result<FetchGacNewsResult>.Success(new FetchGacNewsResult(0, 0, 0, 0, perProvider));
        }

        Result<IngestGacNewsItemsResult> ingest = await _sender.Send(new IngestGacNewsItemsCommand(batch), cancellationToken);
        IngestGacNewsItemsResult summary = ingest.Value!;

        return Result<FetchGacNewsResult>.Success(new FetchGacNewsResult(
            collected.Count, summary.Inserted, summary.Updated, summary.Skipped, perProvider));
    }

    private static IngestGacNewsItem ToIngestItem(FetchedNewsItem item) => new(
        item.Title,
        item.Body,
        item.SourceUrl,
        item.SourceName,
        item.PublishedAt,
        Kind: null,
        Category: null,
        Tags: new[] { item.ProviderKey });

    /// <summary>Queries every enabled provider for every term.</summary>
    /// <param name="terms">The search terms.</param>
    /// <param name="withinDays">The lookback window in days.</param>
    /// <param name="perProvider">Accumulates each provider's item count, including zero.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>Every item returned, before deduplication.</returns>
    private async Task<List<FetchedNewsItem>> CollectAsync(
        IReadOnlyList<string> terms,
        int withinDays,
        Dictionary<string, int> perProvider,
        CancellationToken cancellationToken)
    {
        var collected = new List<FetchedNewsItem>();

        foreach (INewsSourceProvider provider in _providers)
        {
            perProvider.TryAdd(provider.ProviderKey, 0);

            foreach (var term in terms.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var query = new NewsSourceQuery(
                    term.Trim(), _settings.Language, _settings.Region, withinDays, _settings.MaxItemsPerTerm);

                IReadOnlyList<FetchedNewsItem> items = await provider.FetchAsync(query, cancellationToken);
                perProvider[provider.ProviderKey] += items.Count;
                collected.AddRange(items);
            }
        }

        return collected;
    }
}
