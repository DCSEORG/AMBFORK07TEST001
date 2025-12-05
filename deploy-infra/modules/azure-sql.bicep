@description('Azure region for the resources')
param location string = resourceGroup().location

@description('Base name for SQL resources')
param baseName string

@description('Unique suffix for resource naming')
param uniqueSuffix string

@description('Azure AD administrator Object ID')
param adminObjectId string

@description('Azure AD administrator login name (UPN or display name)')
param adminLogin string

@description('Azure AD administrator principal type')
@allowed(['User', 'Application'])
param adminPrincipalType string = 'User'

@description('Managed Identity Principal ID for database access')
param managedIdentityPrincipalId string

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: 'sql-${baseName}-${uniqueSuffix}'
  location: location
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: adminPrincipalType
      login: adminLogin
      sid: adminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

// Allow Azure services to access the SQL Server
resource sqlServerFirewallRule 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: 'Northwind'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
  }
}

@description('The fully qualified domain name of the SQL Server')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('The name of the SQL Server')
output sqlServerName string = sqlServer.name

@description('The name of the SQL Database')
output sqlDatabaseName string = sqlDatabase.name
