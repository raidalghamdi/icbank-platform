namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>The selectable <see cref="Icbank.Platform.Application.Storage"/> port backend.</summary>
public enum ObjectStorageProvider
{
    /// <summary>Local filesystem-backed storage. Default; no cloud dependency.</summary>
    FileSystem = 0,

    /// <summary>Azure Blob Storage, reached via the API's managed identity.</summary>
    AzureBlob = 1,
}
