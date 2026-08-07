namespace Icbank.Platform.Application.Gac.News;

/// <summary>Search parameters passed to an <see cref="Common.Interfaces.INewsSourceProvider"/>.</summary>
/// <param name="Term">The search term, e.g. <c>هيئة المنافسة العامة</c>.</param>
/// <param name="Language">The ISO 639-1 language code to restrict results to, e.g. <c>ar</c>.</param>
/// <param name="Region">The ISO 3166-1 alpha-2 region to bias results toward, e.g. <c>SA</c>.</param>
/// <param name="WithinDays">How far back to search, in days.</param>
/// <param name="MaxItems">The upper bound on items to return for this term.</param>
public sealed record NewsSourceQuery(string Term, string Language, string Region, int WithinDays, int MaxItems);
