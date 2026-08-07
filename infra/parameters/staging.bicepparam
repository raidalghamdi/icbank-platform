using '../main.bicep'

// staging environment — mirrors prod topology at reduced capacity, so a deploy that works here
// has already exercised the same Bicep modules and role assignments prod will use.

param environmentName = 'staging'
param namePrefix = 'icbank'
param location = 'uaenorth'

// REPLACE: Entra ID object ID / UPN / tenant ID of the group or user that will administer SQL
// for this environment.
param sqlEntraAdminObjectId = '00000000-0000-0000-0000-000000000000'
param sqlEntraAdminLogin = 'REPLACE-WITH-ENTRA-ADMIN-GROUP-NAME'
param sqlEntraAdminTenantId = '00000000-0000-0000-0000-000000000000'

param sqlFirewallAllowedRanges = []
param allowAzureServicesToReachSql = true

param sqlDatabaseSku = 'GP_S_Gen5_1'
param sqlDatabaseMaxSizeGb = 32

param appServicePlanSku = 'P0v3'
param appServiceInstanceCount = 1
param dotnetVersion = 'DOTNETCORE|8.0'

param keyVaultSku = 'standard'
param keyVaultSoftDeleteRetentionDays = 30
param enableKeyVaultPurgeProtection = true

param storageAccountSku = 'Standard_LRS'

param logAnalyticsDailyQuotaGb = 2
param logRetentionDays = 60

param tags = {
  application: 'icbank-platform'
  managedBy: 'bicep'
  environment: 'staging'
  costCenter: 'REPLACE-WITH-COST-CENTER'
}
