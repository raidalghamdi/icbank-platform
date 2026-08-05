namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>Strongly-typed binding of the <c>ObjectStorage</c> configuration section.</summary>
public sealed class ObjectStorageOptions
{
    /// <summary>Gets or sets the configuration section name.</summary>
    public const string SectionName = "ObjectStorage";

    /// <summary>Gets or sets the local filesystem root objects are read from.</summary>
    public string RootPath { get; set; } = "App_Data/storage";

    /// <summary>Gets or sets the base URL presigned upload URLs are issued under.</summary>
    public string UploadUrlBase { get; set; } = "https://storage.local/upload";
}
