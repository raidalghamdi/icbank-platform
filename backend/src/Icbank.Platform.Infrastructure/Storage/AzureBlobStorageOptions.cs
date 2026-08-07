namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>Strongly-typed binding of the <c>ObjectStorage:AzureBlob</c> configuration section.</summary>
public sealed class AzureBlobStorageOptions
{
    /// <summary>Gets or sets the configuration section name.</summary>
    public const string SectionName = "ObjectStorage:AzureBlob";

    /// <summary>Gets or sets the Blob service endpoint (e.g. <c>https://icbankdevstorage.blob.core.windows.net</c>).</summary>
    public string ServiceUri { get; set; } = string.Empty;

    /// <summary>Gets or sets the storage account name, needed to build user-delegation SAS query parameters.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets how many minutes a presigned upload URL remains valid. Kept short-lived per BUSINESS-RULES.md §12.3.</summary>
    public int UploadUrlLifetimeMinutes { get; set; } = 15;
}
