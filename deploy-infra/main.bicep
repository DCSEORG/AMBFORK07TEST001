@description('Azure region for the resources')
param location string = resourceGroup().location

@description('Base name for all resources')
param baseName string = 'expensemgmt'

@description('Deploy GenAI resources (Azure OpenAI and AI Search)')
param deployGenAI bool = false

@description('Azure AD administrator Object ID')
param adminObjectId string

@description('Azure AD administrator login name')
param adminLogin string

@description('Azure AD administrator principal type')
@allowed(['User', 'Application'])
param adminPrincipalType string = 'User'

// Generate unique suffix for resource naming
var uniqueSuffix = uniqueString(resourceGroup().id)
var timestamp = '${substring(uniqueSuffix, 0, 6)}'

// Deploy Managed Identity first (needed by other resources)
module managedIdentity 'modules/managed-identity.bicep' = {
  name: 'managedIdentity-deployment'
  params: {
    location: location
    baseName: baseName
    timestamp: timestamp
  }
}

// Deploy Monitoring resources
module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-deployment'
  params: {
    location: location
    baseName: baseName
    uniqueSuffix: uniqueSuffix
    appServiceId: appService.outputs.webAppId
    sqlDatabaseId: resourceId('Microsoft.Sql/servers/databases', azureSQL.outputs.sqlServerName, azureSQL.outputs.sqlDatabaseName)
  }
}

// Deploy App Service
module appService 'modules/app-service.bicep' = {
  name: 'appService-deployment'
  params: {
    location: location
    baseName: baseName
    uniqueSuffix: uniqueSuffix
    managedIdentityId: managedIdentity.outputs.managedIdentityId
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
  }
}

// Deploy Azure SQL
module azureSQL 'modules/azure-sql.bicep' = {
  name: 'azureSQL-deployment'
  params: {
    location: location
    baseName: baseName
    uniqueSuffix: uniqueSuffix
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    adminPrincipalType: adminPrincipalType
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// Conditionally deploy GenAI resources
module genAI 'modules/genai.bicep' = if (deployGenAI) {
  name: 'genAI-deployment'
  params: {
    location: location
    baseName: baseName
    uniqueSuffix: uniqueSuffix
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// Outputs
@description('The name of the App Service')
output webAppName string = appService.outputs.webAppName

@description('The default hostname of the App Service')
output webAppHostName string = appService.outputs.webAppHostName

@description('The fully qualified domain name of the SQL Server')
output sqlServerFqdn string = azureSQL.outputs.sqlServerFqdn

@description('The name of the SQL Server')
output sqlServerName string = azureSQL.outputs.sqlServerName

@description('The name of the SQL Database')
output sqlDatabaseName string = azureSQL.outputs.sqlDatabaseName

@description('The client ID of the managed identity')
output managedIdentityClientId string = managedIdentity.outputs.managedIdentityClientId

@description('The name of the managed identity')
output managedIdentityName string = managedIdentity.outputs.managedIdentityName

@description('Application Insights connection string')
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString

@description('The endpoint of the Azure OpenAI service (empty if not deployed)')
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''

@description('The name of the deployed OpenAI model (empty if not deployed)')
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''

@description('The name of the Azure OpenAI resource (empty if not deployed)')
output openAIName string = deployGenAI ? genAI.outputs.openAIName : ''

@description('The endpoint of the Azure AI Search service (empty if not deployed)')
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''

@description('The name of the Azure AI Search resource (empty if not deployed)')
output searchName string = deployGenAI ? genAI.outputs.searchName : ''
