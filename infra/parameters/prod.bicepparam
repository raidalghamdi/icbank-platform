using '../main.bicep'

// prod environment — purge protection ON (irreversible; Key Vault cannot be fully deleted for
// keyVaultSoftDeleteRetentionDays after deletion, by design), larger SQL/App Service SKUs,
// longer retention. Every REPLACE value below is a hard requirement, not a suggestion — the
// deploying operator must fill these in from the org's real Entra ID tenant before running this
// against a production subscription. See docs/DEPLOYMENT.md "Production checklist".

param environmentName = 'prod'
param namePrefix = 'icbank'
param location = 'uaenorth'

// REPLACE: Entra ID object ID / UPN / tenant ID of the group that will administer SQL in
// production. Strongly recommend a group (e.g. "icbank-platform-sql-admins"), not an individual
// user, so admin access survives personnel changes without a redeploy.
param sqlEntraAdminObjectId = '00000000-0000-0000-0000-000000000000'
param sqlEntraAdminLogin = 'REPLACE-WITH-ENTRA-ADMIN-GROUP-NAME'
param sqlEntraAdminTenantId = '00000000-0000-0000-0000-000000000000'

// Prod: the CD pipeline reaches SQL only to run EF migrations, from GitHub-hosted runners with
// dynamic IPs — allowAzureServicesToReachSql covers Azure-hosted callers (the App Service
// itself does NOT need this rule; it authenticates via managed identity over the public endpoint
// same as everything else here) but GitHub Actions runners are NOT an Azure service and are NOT
// covered by it. See docs/DEPLOYMENT.md "Letting the CD pipeline reach SQL" for the two
// supported options (temporary firewall rule opened/closed by the workflow, or a self-hosted
// runner with a static/known IP added to sqlFirewallAllowedRanges here).
param sqlFirewallAllowedRanges = []
param allowAzureServicesToReachSql = true

param sqlDatabaseSku = 'GP_Gen5_2'
param sqlDatabaseMaxSizeGb = 64

param appServicePlanSku = 'P1v3'
param appServiceInstanceCount = 2
param dotnetVersion = 'DOTNETCORE:8.0'

param keyVaultSku = 'standard'
param keyVaultSoftDeleteRetentionDays = 90
param enableKeyVaultPurgeProtection = true

param storageAccountSku = 'Standard_ZRS' // zone-redundant in prod for higher durability than dev/staging's LRS

param logAnalyticsDailyQuotaGb = 5
param logRetentionDays = 180

param tags = {
  application: 'icbank-platform'
  managedBy: 'bicep'
  environment: 'prod'
  costCenter: 'REPLACE-WITH-COST-CENTER'
}
