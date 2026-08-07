namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /weekend-places</c>.</summary>
public sealed class CreateWeekendPlaceRequest
{
    /// <summary>Gets or sets the place name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the place description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the image URL, if any.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the city (defaults to Riyadh if omitted).</summary>
    public string? City { get; set; }

    /// <summary>Gets or sets the Google Maps query, if any.</summary>
    public string? MapsQuery { get; set; }

    /// <summary>Gets or sets the display sort order.</summary>
    public int SortOrder { get; set; }
}
