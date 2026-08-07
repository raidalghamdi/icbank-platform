namespace Icbank.Platform.Domain.Gac;

/// <summary>Media kind attached to a cached social post (DATA-MODEL.md section 5).</summary>
public enum GacSocialMediaType
{
    /// <summary>No media attached.</summary>
    None = 0,

    /// <summary>An image is attached.</summary>
    Image = 1,

    /// <summary>A video is attached.</summary>
    Video = 2,
}
