namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="GacController.FetchNewsAsync"/>.</summary>
/// <param name="Terms">Optional search-term override for this run; omit to use the configured terms.</param>
/// <param name="WithinDays">Optional lookback-window override in days; omit to use the configured window.</param>
public sealed record FetchGacNewsRequest(IReadOnlyList<string>? Terms, int? WithinDays);
