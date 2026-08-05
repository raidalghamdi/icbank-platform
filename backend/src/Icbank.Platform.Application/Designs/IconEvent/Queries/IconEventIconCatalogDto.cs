namespace Icbank.Platform.Application.Designs.IconEvent.Queries;

/// <summary>The full icon-catalogue response shape (ports the Node source's <c>{icons,count}</c>).</summary>
/// <param name="Icons">Every catalogued icon.</param>
/// <param name="Count">The total icon count.</param>
public sealed record IconEventIconCatalogDto(IReadOnlyList<IconEventIconDto> Icons, int Count);
