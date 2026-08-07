namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>Strongly-typed binding of the <c>ObjectStorage</c> configuration section.</summary>
public sealed class ObjectStorageOptions
{
    /// <summary>Gets or sets the configuration section name.</summary>
    public const string SectionName = "ObjectStorage";

    /// <summary>
    /// Gets or sets which backend implementation to register: <see cref="ObjectStorageProvider.FileSystem"/>
    /// (default -- no cloud dependency, used for local development and the test suite) or
    /// <see cref="ObjectStorageProvider.AzureBlob"/> (deployed environments; app-service.bicep sets
    /// this to <c>AzureBlob</c> via the <c>ObjectStorage__Provider</c> app setting).
    /// </summary>
    public ObjectStorageProvider Provider { get; set; } = ObjectStorageProvider.FileSystem;

    /// <summary>Gets or sets the local filesystem root objects are read from.</summary>
    public string RootPath { get; set; } = "App_Data/storage";

    /// <summary>Gets or sets the base URL presigned upload URLs are issued under.</summary>
    public string UploadUrlBase { get; set; } = "https://storage.local/upload";
}
