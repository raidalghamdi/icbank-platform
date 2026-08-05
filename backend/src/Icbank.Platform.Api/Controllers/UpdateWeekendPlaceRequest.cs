namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>PATCH /weekend-places/:id</c>. Every field is optional (partial update).</summary>
public sealed class UpdateWeekendPlaceRequest
{
    /// <summary>Gets or sets the new name, if changing.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the new description, if changing.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the new image URL, if changing.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the new city, if changing.</summary>
    public string? City { get; set; }

    /// <summary>Gets or sets the new Maps query, if changing.</summary>
    public string? MapsQuery { get; set; }

    /// <summary>Gets or sets the new active flag, if changing.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Gets or sets the new sort order, if changing.</summary>
    public int? SortOrder { get; set; }
}
