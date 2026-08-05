// Azure SQL Database — logical server + single database, Entra-ID-only authentication.
//
// azureADOnlyAuthentication is set to true: no SQL-auth admin login/password pair is ever
// created, so there is no SQL credential to leak in the first place — the server can only be
// administered via Entra ID, and the API reaches it via its managed identity (granted a
// contained-database-user, see the T-SQL note in main.bicep and docs/DEPLOYMENT.md — Azure
// Resource Manager has no declarative resource for CREATE USER FROM EXTERNAL PROVIDER).
param baseName string
param location string
param entraAdminObjectId string
param entraAdminLogin string
param entraAdminTenantId string
param firewallAllowedRanges array
param allowAzureServices bool
param databaseSku string
param databaseMaxSizeGb int
param tags object

var serverName = '${baseName}-sql'
var databaseName = '${baseName}-db'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  tags: tags
  properties: {
    // Why: no `administratorLogin`/`administratorLoginPassword` — Entra-ID-only auth means
    // there is no SQL-auth credential for this template, or anything downstream of it, to hold.
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'Group'
      login: entraAdminLogin
      sid: entraAdminObjectId
      tenantId: entraAdminTenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: databaseSku
  }
  properties: {
    maxSizeBytes: databaseMaxSizeGb * 1024 * 1024 * 1024
    zoneRedundant: false
    requestedBackupStorageRedundancy: 'Local'
  }
}

resource allowAzureServicesRule 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = if (allowAzureServices) {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource clientFirewallRules 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = [for (range, i) in firewallAllowedRanges: {
  parent: sqlServer
  name: 'AllowedRange-${i}'
  properties: {
    startIpAddress: range.start
    endIpAddress: range.end
  }
}]

// Auditing to the same Log Analytics workspace would be a natural addition; deferred (not
// enabled by default) because it requires a storage account or Log Analytics linked-service
// target and was judged out of scope for the initial provisioning pass — see
// spec/AZURE-NOTES.md "Deliberately deferred" section.

output serverName string = sqlServer.name
output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
