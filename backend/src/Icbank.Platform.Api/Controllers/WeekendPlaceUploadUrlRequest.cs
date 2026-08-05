namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /weekend-places/upload-url</c>.</summary>
public sealed class WeekendPlaceUploadUrlRequest
{
    /// <summary>Gets or sets the client-supplied original file name.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional MIME content type.</summary>
    public string? ContentType { get; set; }
}
