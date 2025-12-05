@description('Azure region for the resources')
param location string = resourceGroup().location

@description('Base name for the managed identity')
param baseName string

@description('Timestamp for uniqueness')
param timestamp string

// User-assigned managed identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mid-${baseName}-${timestamp}'
  location: location
}

@description('The resource ID of the managed identity')
output managedIdentityId string = managedIdentity.id

@description('The client ID of the managed identity')
output managedIdentityClientId string = managedIdentity.properties.clientId

@description('The principal ID of the managed identity')
output managedIdentityPrincipalId string = managedIdentity.properties.principalId

@description('The name of the managed identity')
output managedIdentityName string = managedIdentity.name
