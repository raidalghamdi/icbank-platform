namespace Icbank.Platform.Application.Common.Models;

/// <summary>
/// Standard pagination envelope for every collection endpoint (R-BE-033, mirrored by R-FE-025).
/// </summary>
/// <typeparam name="T">The item type carried by this page.</typeparam>
/// <param name="Items">The items belonging to the requested page.</param>
/// <param name="Page">The 1-based page number that was returned.</param>
/// <param name="PageSize">The number of items requested per page.</param>
/// <param name="Total">The total number of items across all pages.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
