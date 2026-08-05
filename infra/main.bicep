// Icbank Platform — Azure infrastructure entry point.
//
// Deploys one environment (dev/staging/prod) at a time via a parameter file under
// infra/parameters/. Every value that could differ between environments or that must never be
// a literal is a parameter with a documented default — see docs/DEPLOYMENT.md for the full
// runbook and spec/AZURE-NOTES.md for the assumptions behind these defaults.
//
// Region default: uae-north (UAE North / "UAE North" — Dubai). The platform is Arabic and
// Saudi-facing, so data residency and latency both favour a Gulf region. uae-north is the
// closest Azure region with full first-party service availability (App Service Linux, Azure
// SQL, Key Vault, Storage, Application Insights) to Saudi Arabia — Azure has never had a KSA
// region. If a specific SKU/feature is unavailable in uae-north at deploy time, the documented
// fallback is westeurope (see docs/DEPLOYMENT.md §"Region fallback"). This is a same-deployment
// parameter, not a hardcoded value: pass -p location=westeurope to fall back.
//
// No secret of any kind appears in this file or in any file it references. Every credential the
// API needs at runtime is either a managed-identity-authenticated connection (SQL, Blob) or a
// Key Vault reference resolved at startup (see backend/src/Icbank.Platform.Api's
// Key Vault configuration provider wiring).
targetScope = 'resourceGroup'

@description('Environment name. Drives resource naming and SKU sizing defaults.')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environmentName string = 'dev'

@description('Short, globally-meaningful prefix for all resource names, e.g. "icbank". Kept short because Storage Account / Key Vault names have tight length limits.')
@minLength(3)
@maxLength(11)
param namePrefix string = 'icbank'

@description('Primary Azure region. Default uae-north for Gulf data residency/latency. Fall back to westeurope if a required SKU/service is unavailable in uae-north — see docs/DEPLOYMENT.md.')
@allowed([
  'uaenorth'
  'westeurope'
])
param location string = 'uaenorth'

@description('Object ID (Entra ID) of the user, group, or service principal to set as the Azure SQL Entra-ID administrator. Must be supplied at deploy time — there is no safe default.')
param sqlEntraAdminObjectId string

@description('Display name (UPN or group name) for the Azure SQL Entra-ID administrator, shown in the Azure portal.')
param sqlEntraAdminLogin string

@description('Entra tenant ID the SQL Entra-ID administrator belongs to.')
param sqlEntraAdminTenantId string

@description('Client (public) IP ranges allowed through the SQL Server firewall, CIDR or single-IP form as start/end pairs. Empty by default — deploy-time operators add their own IP via docs/DEPLOYMENT.md before running migrations from outside Azure.')
param sqlFirewallAllowedRanges array = []

@description('Whether to allow Azure services (App Service, GitHub-hosted OIDC runners are NOT covered by this — see docs) to reach SQL via the 0.0.0.0-0.0.0.0 special rule. Needed because the API and CD pipeline reach SQL over the public endpoint using Entra ID / managed identity auth, not IP-restricted service endpoints, in this design.')
param allowAzureServicesToReachSql bool = true

@description('SQL Database SKU name (vCore or DTU family), e.g. GP_S_Gen5_1 (serverless General Purpose) for dev/staging, GP_Gen5_2 for prod.')
param sqlDatabaseSku string = 'GP_S_Gen5_1'

@description('SQL Database max size in GB.')
param sqlDatabaseMaxSizeGb int = 32

@description('App Service Plan SKU. Linux, per-environment sizing: B1 for dev, P0v3 for staging, P1v3 for prod are the documented recommendations; the parameter has no environment-conditional default so every environment states it explicitly in its .bicepparam file.')
param appServicePlanSku string = 'B1'

@description('Number of App Service worker instances.')
@minValue(1)
@maxValue(10)
param appServiceInstanceCount int = 1

@description('.NET runtime version identifier for the Linux App Service site.')
param dotnetVersion string = 'DOTNETCORE:8.0'

@description('Azure Communication Services Email endpoint (e.g. https://<acs-resource>.communication.azure.com). Not a secret. Provisioning the Communication Services resource itself is a documented manual step in docs/DEPLOYMENT.md, outside this template\'s scope -- left blank until an operator supplies it.')
param acsEmailEndpoint string = ''

@description('The verified sender email address (or MailFrom domain address) configured on the Communication Services Email domain, e.g. DoNotReply@<verified-domain>.')
param acsEmailSenderAddress string = ''

@description('Key Vault SKU.')
@allowed([
  'standard'
  'premium'
])
param keyVaultSku string = 'standard'

@description('Number of days soft-deleted Key Vault items are retained.')
@minValue(7)
@maxValue(90)
param keyVaultSoftDeleteRetentionDays int = 90

@description('Whether Key Vault purge protection is enabled. Irreversible once turned on — recommended true for staging/prod, false for dev so throwaway environments can be fully deleted.')
param enableKeyVaultPurgeProtection bool = false

@description('Storage account SKU for the Blob Storage account used for object uploads/reports.')
param storageAccountSku string = 'Standard_LRS'

@description('Log Analytics workspace daily ingestion cap in GB. -1 disables the cap (not recommended for prod cost control, but the documented dev/staging default is small to keep cost bounded).')
param logAnalyticsDailyQuotaGb int = 1

@description('Log retention in days for both Log Analytics and Application Insights.')
@minValue(30)
@maxValue(730)
param logRetentionDays int = 90

@description('Tags applied to every resource, for cost tracking and ownership.')
param tags object = {
  application: 'icbank-platform'
  managedBy: 'bicep'
  environment: environmentName
}

// ---------------------------------------------------------------------------
// Naming — centralized so every module derives consistent, environment-scoped names.
// Storage accounts and Key Vaults have the tightest constraints (Storage: 3-24 lowercase
// alphanumeric, globally unique; Key Vault: 3-24 alphanumeric, globally unique), so both use a
// hash suffix derived from the resource group ID to stay under length limits while remaining
// deterministic across repeated deployments of the same environment.
var uniqueSuffix = uniqueString(resourceGroup().id, environmentName)
var resourceBaseName = '${namePrefix}-${environmentName}'
var storageAccountName = toLower('${namePrefix}${environmentName}${uniqueSuffix}')
var keyVaultName = toLower('kv-${namePrefix}-${environmentName}-${substring(uniqueSuffix, 0, 6)}')

module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'logAnalytics'
  params: {
    name: '${resourceBaseName}-law'
    location: location
    dailyQuotaGb: logAnalyticsDailyQuotaGb
    retentionInDays: logRetentionDays
    tags: tags
  }
}

module appInsights 'modules/app-insights.bicep' = {
  name: 'appInsights'
  params: {
    name: '${resourceBaseName}-appi'
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    retentionInDays: logRetentionDays
    tags: tags
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault'
  params: {
    name: keyVaultName
    location: location
    skuName: keyVaultSku
    softDeleteRetentionInDays: keyVaultSoftDeleteRetentionDays
    enablePurgeProtection: enableKeyVaultPurgeProtection
    tags: tags
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    name: storageAccountName
    location: location
    skuName: storageAccountSku
    tags: tags
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    baseName: resourceBaseName
    location: location
    entraAdminObjectId: sqlEntraAdminObjectId
    entraAdminLogin: sqlEntraAdminLogin
    entraAdminTenantId: sqlEntraAdminTenantId
    firewallAllowedRanges: sqlFirewallAllowedRanges
    allowAzureServices: allowAzureServicesToReachSql
    databaseSku: sqlDatabaseSku
    databaseMaxSizeGb: sqlDatabaseMaxSizeGb
    tags: tags
  }
}

module appService 'modules/app-service.bicep' = {
  name: 'appService'
  params: {
    baseName: resourceBaseName
    location: location
    planSku: appServicePlanSku
    instanceCount: appServiceInstanceCount
    dotnetVersion: dotnetVersion
    keyVaultUri: keyVault.outputs.vaultUri
    appInsightsConnectionString: appInsights.outputs.connectionString
    sqlServerFqdn: sql.outputs.serverFqdn
    sqlDatabaseName: sql.outputs.databaseName
    storageAccountBlobEndpoint: storage.outputs.blobEndpoint
    storageAccountName: storage.outputs.name
    environmentName: environmentName
    acsEmailEndpoint: acsEmailEndpoint
    acsEmailSenderAddress: acsEmailSenderAddress
    tags: tags
  }
}

// ---------------------------------------------------------------------------
// Role assignments — the API's managed identity is granted the minimum roles needed to reach
// SQL (via Entra ID auth, no connection-string password), Blob Storage, and Key Vault. No
// connection string or access key is ever written to App Service configuration for these three
// services; only the resource URIs (which are not secrets) are.

module keyVaultAccess 'modules/key-vault-access.bicep' = {
  name: 'keyVaultAccess'
  params: {
    keyVaultName: keyVault.outputs.name
    principalId: appService.outputs.principalId
  }
}

module storageAccess 'modules/storage-access.bicep' = {
  name: 'storageAccess'
  params: {
    storageAccountName: storage.outputs.name
    principalId: appService.outputs.principalId
  }
}

// SQL database-level role (db_datareader/db_datawriter equivalent via contained Entra user) is
// NOT created by this Bicep file: Azure SQL contained-database-user creation for a managed
// identity requires a T-SQL statement executed against the database itself (CREATE USER FROM
// EXTERNAL PROVIDER), which ARM/Bicep's Microsoft.Sql RP has no declarative resource for. This
// is documented, not swept under the rug — see docs/DEPLOYMENT.md §"Grant the API's managed
// identity access to SQL" for the exact T-SQL and the deploy step that must run it once per
// environment after this template applies.

output resourceGroupName string = resourceGroup().name
output appServiceName string = appService.outputs.name
output appServiceDefaultHostname string = appService.outputs.defaultHostname
output appServicePrincipalId string = appService.outputs.principalId
output keyVaultName string = keyVault.outputs.name
output keyVaultUri string = keyVault.outputs.vaultUri
output sqlServerFqdn string = sql.outputs.serverFqdn
output sqlServerName string = sql.outputs.serverName
output sqlDatabaseName string = sql.outputs.databaseName
output storageAccountName string = storage.outputs.name
output storageBlobEndpoint string = storage.outputs.blobEndpoint
output appInsightsConnectionString string = appInsights.outputs.connectionString
output logAnalyticsWorkspaceId string = logAnalytics.outputs.workspaceId
