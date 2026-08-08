namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="IconEventDesignsController.GenerateAsync"/>.</summary>
/// <param name="RawData">The raw free-text event data, if supplied.</param>
/// <param name="Headline">The explicit headline override.</param>
/// <param name="Subtitle">The explicit subtitle override.</param>
/// <param name="Department">The explicit, confirmed department name.</param>
/// <param name="Hashtag">The explicit, confirmed hashtag.</param>
/// <param name="Date">The event date.</param>
/// <param name="Time">The event time.</param>
/// <param name="Location">The event location.</param>
/// <param name="EventType">The event type, used for the local-fallback icon choice.</param>
/// <param name="Size">The preset the three style previews are drawn at; optional, defaults to <c>desktop-hd</c>.</param>
/// <param name="MainIconOverride">The user's explicit icon choice.</param>
public sealed record GenerateIconEventDesignRequest(
    string? RawData,
    string? Headline,
    string? Subtitle,
    string? Department,
    string? Hashtag,
    string? Date,
    string? Time,
    string? Location,
    string? EventType,
    string? Size,
    string? MainIconOverride);
