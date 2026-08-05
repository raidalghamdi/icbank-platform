// Key Vault — holds every secret the API needs at runtime that is NOT reachable via managed
// identity (SQL and Blob are managed-identity-authenticated and therefore need no secret at
// all): Jwt:SigningKey, Cron:ApiKey, the Azure Communication Services connection string, and any
// AzureAd client secret if Entra ID SSO is enabled for the environment.
//
// RBAC authorization model (enableRbacAuthorization: true) is used instead of the legacy access
// policy model, per Microsoft's current guidance and because it lets the API's managed identity
// be granted a single built-in "Key Vault Secrets User" role instead of a hand-rolled access
// policy — see modules/key-vault-access.bicep.
//
// No secret is set by this module. Populating actual secret VALUES is a manual, documented,
// one-time-per-environment operator step (docs/DEPLOYMENT.md) — Bicep only creates the empty
// vault and its access wiring, exactly per the "no secret in any Bicep file" requirement.
param name string
param location string
param skuName string
param softDeleteRetentionInDays int

@description('Purge protection: cannot be disabled once turned on, so this is off for dev to allow throwaway environments to be fully deleted, and should be turned on for prod via the prod parameter file.')
param enablePurgeProtection bool = false
param tags object

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: skuName
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enablePurgeProtection: enablePurgeProtection ? true : null
    enabledForDeployment: false
    enabledForTemplateDeployment: false
    enabledForDiskEncryption: false
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

output name string = keyVault.name
output vaultUri string = keyVault.properties.vaultUri
output id string = keyVault.id
