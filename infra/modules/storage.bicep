// Azure Blob Storage — replaces the filesystem-backed object storage placeholder
// (FileSystemObjectStorageReader/Writer/FileSystemObjectUploadUrlIssuer) in a deployed
// environment. Access from the API is exclusively via managed identity (Storage Blob Data
// Contributor role, granted in modules/storage-access.bicep) — no account key or connection
// string is ever generated or stored for the API's own use. User-delegation SAS tokens (used for
// short-lived client presigned upload URLs) are minted at runtime using the API's managed
// identity credentials, which requires no key at all — see
// Icbank.Platform.Infrastructure/Storage/AzureBlobObjectUploadUrlIssuer.cs.
param name string
param location string
param skuName string
param tags object

@description('Blob container names created for the platform\'s object-storage prefixes (mirrors the folder-prefix convention already used by the filesystem implementation: weekend/, designs/, shorfah/, media-reports/, ai-year/).')
var containerNames = [
  'weekend'
  'designs'
  'shorfah'
  'media-reports'
  'ai-year'
]

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true // Kept enabled for local/dev-tooling emergency access; the API itself never uses a key, only managed identity + user-delegation SAS.
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 14
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 14
    }
  }
}

resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = [for containerName in containerNames: {
  parent: blobService
  name: containerName
  properties: {
    publicAccess: 'None'
  }
}]

output name string = storageAccount.name
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
output id string = storageAccount.id
