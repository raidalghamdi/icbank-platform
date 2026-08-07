using Icbank.Platform.Application.Gac.News;

namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// A pluggable outward port for retrieving press coverage from one upstream news source.
/// </summary>
/// <remarks>
/// This exists because no single provider covers the Saudi business press that reports on the
/// Authority. An August 2026 evaluation measured Google News (<c>hl=ar&amp;gl=SA</c>) returning
/// 47-61 items for <c>هيئة المنافسة</c> over 30 days from أرقام, صحيفة مال, الاقتصادية, سبق and
/// أخبار 24, while NewsData.io returned zero for the same query and does not index any of those
/// five outlets. Rather than hard-wiring the winner, every provider implements this port and is
/// selected by configuration, so swapping in a licensed regional feed later is a config change
/// plus one new class -- not a rewrite of the ingest pipeline.
/// </remarks>
public interface INewsSourceProvider
{
    /// <summary>Gets the stable configuration key identifying this provider (e.g. <c>google-news-rss</c>).</summary>
    string ProviderKey { get; }

    /// <summary>Retrieves press items matching a query.</summary>
    /// <param name="query">The search parameters.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>
    /// The items found, newest first. Returns an empty list rather than throwing when the upstream
    /// is unreachable or returns an unparseable payload, so one failing provider cannot abort a
    /// multi-provider fetch.
    /// </returns>
    Task<IReadOnlyList<FetchedNewsItem>> FetchAsync(NewsSourceQuery query, CancellationToken cancellationToken);
}
