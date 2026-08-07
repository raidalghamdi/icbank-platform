using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Weekend;

/// <summary>Admin-curated library of Riyadh venues/places shown on the weekend page (DATA-MODEL.md section 3.10 <c>weekend_places</c>).</summary>
public sealed class WeekendPlace : AuditableEntity
{
    /// <summary>Gets or sets the place name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the place description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the image URL, if any.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the city, hardcoded to Riyadh by default in the source system.</summary>
    public string City { get; set; } = "الرياض";

    /// <summary>Gets or sets a Google Maps query string, if any.</summary>
    public string? MapsQuery { get; set; }

    /// <summary>Gets or sets a value indicating whether the place is currently shown.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the display sort order.</summary>
    public int SortOrder { get; set; }
}
