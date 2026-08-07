// Application Insights, workspace-based (the only mode Microsoft now recommends — classic
// non-workspace App Insights is deprecated). The connection string this emits is not a secret in
// the "grants access to other resources" sense (it only lets the SDK write telemetry to this
// specific resource), so it is passed to the API as a plain app setting rather than routed
// through Key Vault — see docs/DEPLOYMENT.md for the explicit reasoning.
param name string
param location string
param logAnalyticsWorkspaceId string
param retentionInDays int
param tags object

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
    IngestionMode: 'LogAnalytics'
    RetentionInDays: retentionInDays
  }
}

output connectionString string = appInsights.properties.ConnectionString
output instrumentationKey string = appInsights.properties.InstrumentationKey
output name string = appInsights.name
