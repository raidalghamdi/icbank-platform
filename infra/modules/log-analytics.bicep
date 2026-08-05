// Log Analytics workspace — backing store for Application Insights and App Service diagnostic
// logs. No secrets: workspace keys are not consumed anywhere in this stack (App Insights uses a
// connection string that is a resource identifier, not a shared secret, and is passed to the app
// as a Key-Vault-free app setting since it grants no read access to anything by itself).
param name string
param location string
param dailyQuotaGb int
param retentionInDays int
param tags object

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionInDays
    workspaceCapping: {
      dailyQuotaGb: dailyQuotaGb
    }
  }
}

output workspaceId string = workspace.id
output name string = workspace.name
