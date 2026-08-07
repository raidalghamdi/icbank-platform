namespace Icbank.Platform.Application.Gac.News;

/// <summary>
/// One press item as returned by a provider, before it is mapped onto
/// <see cref="Domain.Gac.GacNewsItem"/> and persisted.
/// </summary>
/// <param name="Title">The headline, with any trailing " - Outlet" suffix already stripped.</param>
/// <param name="Body">The article body when the provider supplies one; null for headline-only feeds.</param>
/// <param name="SourceUrl">The canonical article URL. Doubles as the deduplication key.</param>
/// <param name="SourceName">The publishing outlet's display name, e.g. <c>صحيفة مال</c>.</param>
/// <param name="PublishedAt">The publication timestamp when the provider supplies one.</param>
/// <param name="ProviderKey">The <see cref="Common.Interfaces.INewsSourceProvider.ProviderKey"/> that produced this item.</param>
public sealed record FetchedNewsItem(
    string Title,
    string? Body,
    string SourceUrl,
    string SourceName,
    DateTimeOffset? PublishedAt,
    string ProviderKey);
