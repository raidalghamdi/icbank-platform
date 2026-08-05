// Grants the API's system-assigned managed identity the built-in "Key Vault Secrets User" role
// (read-only secret access — GET/LIST, never write) on the vault, scoped to this vault only.
// Requires enableRbacAuthorization: true on the vault (see modules/key-vault.bicep).
param keyVaultName string
param principalId string

@description('Built-in role definition ID for "Key Vault Secrets User" — read-only secret access. See https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#key-vault-secrets-user')
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, principalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
