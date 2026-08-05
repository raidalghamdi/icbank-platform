// App Service on Linux — hosts the .NET 8 API.
//
// Decision: App Service (Linux, native .NET runtime stack) over Azure Container Apps.
// Justification (expanded in docs/DEPLOYMENT.md):
//   1. The API is a single stateless ASP.NET Core process with no sidecar/multi-container need
//      today — Container Apps' main advantage (Dapr, KEDA scale-to-zero, multi-container pods)
//      is not used by this codebase, so it would add operational surface without benefit.
//   2. App Service's native "deploy a zip/artifact built by `dotnet publish`" model matches this
//      repo's CD pipeline (build → gate → publish artifact → deploy) without requiring a
//      container registry, an extra build stage to produce/push an image, or image-vulnerability
//      scanning infrastructure this task's budget does not cover.
//   3. App Service's built-in health-check-path integration, deployment slots (for blue/green,
//      addable later without changing this template's shape), and first-party Key-Vault-reference
//      app settings are more mature than Container Apps' equivalents as of this writing.
//   4. Scale-to-zero (Container Apps' strongest differentiator) is not a requirement here — this
//      is an always-on internal/admin platform API, not a bursty public workload.
// If the platform later adds background workers, multiple services, or needs KEDA-based
// event-driven scaling, Container Apps should be reconsidered — this decision is not permanent.
param baseName string
param location string
param planSku string
param instanceCount int
param dotnetVersion string
param keyVaultUri string
param appInsightsConnectionString string
param sqlServerFqdn string
param sqlDatabaseName string
param storageAccountBlobEndpoint string
param storageAccountName string
param environmentName string
param tags object

var planName = '${baseName}-plan'
var siteName = '${baseName}-api'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: planSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// Why: the SQL connection string carries no password — Authentication=Active Directory Managed
// Identity means the driver obtains a token for the API's own managed identity at connect time.
// This is a connection STRING, not a secret, and is safe as a plain app setting; it contains no
// credential material, which is exactly why it does not go through Key Vault.
var sqlConnectionString = 'Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource site 'Microsoft.Web/sites@2023-12-01' = {
  name: siteName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    keyVaultReferenceIdentity: 'SystemAssigned'
    siteConfig: {
      linuxFxVersion: dotnetVersion
      alwaysOn: planSku != 'F1' && planSku != 'D1'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      healthCheckPath: '/health/ready'
      numberOfWorkers: instanceCount
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName == 'prod' ? 'Production' : (environmentName == 'staging' ? 'Staging' : 'Development')
        }
        {
          name: 'KeyVault__VaultUri'
          value: keyVaultUri
        }
        {
          name: 'ConnectionStrings__Default'
          value: sqlConnectionString
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: appInsightsConnectionString
        }
        {
          name: 'ObjectStorage__Provider'
          value: 'AzureBlob'
        }
        {
          name: 'ObjectStorage__AzureBlob__ServiceUri'
          value: storageAccountBlobEndpoint
        }
        {
          name: 'ObjectStorage__AzureBlob__AccountName'
          value: storageAccountName
        }
        {
          name: 'Notifications__Provider'
          value: 'AzureCommunicationServices'
        }
        {
          name: 'WEBSITE_HTTPLOGGING_RETENTION_DAYS'
          value: '7'
        }
      ]
    }
  }
}

// Why: numberOfWorkers above sets desired instance count for a fixed (non-autoscale) plan;
// autoscale rules are a documented-but-not-provisioned follow-up (see spec/AZURE-NOTES.md)
// since the requested scope is "provision the resources", not "tune capacity", and autoscale
// thresholds need real traffic data this task has no way to obtain without a subscription.

output name string = site.name
output defaultHostname string = site.properties.defaultHostName
output principalId string = site.identity.principalId
