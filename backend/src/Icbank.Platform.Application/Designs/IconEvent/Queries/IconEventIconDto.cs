namespace Icbank.Platform.Application.Designs.IconEvent.Queries;

/// <summary>One entry of the icon catalogue response.</summary>
/// <param name="Name">The stable icon key.</param>
/// <param name="LabelAr">The Arabic display label.</param>
/// <param name="Category">The category, lower-kebab-case to match the Node source's response shape.</param>
/// <param name="Keywords">The Arabic semantic keywords.</param>
public sealed record IconEventIconDto(string Name, string LabelAr, string Category, IReadOnlyList<string> Keywords);
