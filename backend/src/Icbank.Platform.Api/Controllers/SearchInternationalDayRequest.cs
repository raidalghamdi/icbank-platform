using System.Text.Json.Serialization;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="InternationalDaysController.SearchAsync"/>.</summary>
/// <param name="Query">The day name to research.</param>
/// <param name="Category">The optional category to tag the result with.</param>
/// <param name="ForceRefresh">Whether to bypass the 7-day cache.</param>
public sealed record SearchInternationalDayRequest(
    string Query,
    string? Category,
    [property: JsonPropertyName("force_refresh")] bool ForceRefresh);
