@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('App Service Plan name.')
param appServicePlanName string

@description('Web App name.')
param webAppName string

@description('Application Insights name.')
param applicationInsightsName string

@description('App Service plan SKU.')
@allowed([
  'B1'
  'S1'
  'P1v3'
])
param appServicePlanSku string = 'B1'

@description('Windows runtime stack for App Service.')
param netFrameworkVersion string = 'v10.0'

@description('Additional app settings (for example LegacyAppSettings__ClientId).')
param extraAppSettings object = {}

var defaultAppSettings = {
  ASPNETCORE_ENVIRONMENT: 'Production'
  WEBSITE_RUN_FROM_PACKAGE: '1'
  LegacyAppSettings__EnableExtractor: 'false'
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
    tier: appServicePlanSku == 'B1' ? 'Basic' : (appServicePlanSku == 'S1' ? 'Standard' : 'PremiumV3')
    size: appServicePlanSku
    capacity: 1
  }
  properties: {
    reserved: false
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: netFrameworkVersion
      appCommandLine: 'dotnet VstsDemoBuilder.dll'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
  }
}

resource webAppAppSettings 'Microsoft.Web/sites/config@2023-12-01' = {
  name: 'appsettings'
  parent: webApp
  properties: union(defaultAppSettings, extraAppSettings, {
    APPLICATIONINSIGHTS_CONNECTION_STRING: applicationInsights.properties.ConnectionString
    APPINSIGHTS_INSTRUMENTATIONKEY: applicationInsights.properties.InstrumentationKey
  })
}

output webAppName string = webApp.name
output webAppHostName string = webApp.properties.defaultHostName
output appInsightsConnectionString string = applicationInsights.properties.ConnectionString
