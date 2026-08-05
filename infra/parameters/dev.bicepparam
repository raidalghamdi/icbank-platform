using '../main.bicep'

// dev environment — smallest/cheapest SKUs, purge protection off so the whole resource group can
// be torn down and recreated freely. Values below marked "REPLACE" MUST be supplied by the
// deploying operator before running `az deployment group create`; see docs/DEPLOYMENT.md.

param environmentName = 'dev'
param namePrefix = 'icbank'
param location = 'uaenorth'

// REPLACE: Entra ID object ID / UPN / tenant ID of the group or user that will administer SQL
// for this environment. There is no safe default — leaving these unset fails validation.
param sqlEntraAdminObjectId = '00000000-0000-0000-0000-000000000000'
param sqlEntraAdminLogin = 'REPLACE-WITH-ENTRA-ADMIN-GROUP-NAME'
param sqlEntraAdminTenantId = '00000000-0000-0000-0000-000000000000'

// Dev: no static client IP allow-listed by default — add your own via the parameter file or
// `az sql server firewall-rule create` after deployment (see docs/DEPLOYMENT.md).
param sqlFirewallAllowedRanges = []
param allowAzureServicesToReachSql = true

param sqlDatabaseSku = 'GP_S_Gen5_1' // serverless General Purpose, auto-pauses — cheapest option that still supports Entra-ID-only auth
param sqlDatabaseMaxSizeGb = 10

param appServicePlanSku = 'B1'
param appServiceInstanceCount = 1
param dotnetVersion = 'DOTNETCORE:8.0'

param keyVaultSku = 'standard'
param keyVaultSoftDeleteRetentionDays = 7

param storageAccountSku = 'Standard_LRS'

param logAnalyticsDailyQuotaGb = 1
param logRetentionDays = 30

param tags = {
  application: 'icbank-platform'
  managedBy: 'bicep'
  environment: 'dev'
  costCenter: 'REPLACE-WITH-COST-CENTER'
}
