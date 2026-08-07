namespace Icbank.Platform.Application.Gac;

/// <summary>Ports a category-count row for the publications filter chips (API-SURFACE.md §12).</summary>
/// <param name="Category">The category name.</param>
/// <param name="Count">The number of published publications in that category.</param>
public sealed record GacPublicationCategoryCountDto(string Category, int Count);
