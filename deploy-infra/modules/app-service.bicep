@description('Azure region for the resources')
param location string = resourceGroup().location

@description('Base name for the app service')
param baseName string

@description('Unique suffix for resource naming')
param uniqueSuffix string

@description('Resource ID of the user-assigned managed identity')
param managedIdentityId string

@description('Application Insights connection string')
param appInsightsConnectionString string = ''

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'asp-${baseName}-${uniqueSuffix}'
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: 'app-${baseName}-${uniqueSuffix}'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
      ]
    }
  }
}

@description('The resource ID of the App Service')
output webAppId string = appService.id

@description('The name of the App Service')
output webAppName string = appService.name

@description('The default hostname of the App Service')
output webAppHostName string = appService.properties.defaultHostName

@description('The principal ID of the managed identity assigned to App Service')
output managedIdentityPrincipalId string = managedIdentityId
