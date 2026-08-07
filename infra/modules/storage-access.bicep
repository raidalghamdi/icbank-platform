// Grants the API's system-assigned managed identity "Storage Blob Data Contributor" (read/write/
// delete blob data, but NOT account-key or control-plane access) on the storage account. This is
// also the role required to mint user-delegation SAS tokens for the account, which is how the
// presigned-upload-URL flow (AzureBlobObjectUploadUrlIssuer) works without ever generating or
// storing a shared-key-based SAS.
param storageAccountName string
param principalId string

@description('Built-in role definition ID for "Storage Blob Data Contributor". See https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#storage-blob-data-contributor')
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, principalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
