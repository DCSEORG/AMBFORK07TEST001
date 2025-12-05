@description('Azure region for the resources')
param location string = resourceGroup().location

@description('Base name for GenAI resources')
param baseName string

@description('Unique suffix for resource naming')
param uniqueSuffix string

@description('Principal ID of the managed identity to grant access')
param managedIdentityPrincipalId string

// Azure OpenAI (must be in Sweden Central for better quota)
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: toLower('oai-${baseName}-${uniqueSuffix}')
  location: 'swedencentral'
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: toLower('oai-${baseName}-${uniqueSuffix}')
    publicNetworkAccess: 'Enabled'
  }
}

// Deploy GPT-4o model
resource openAIDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: 'gpt-4o'
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-08-06'
    }
  }
}

// Azure AI Search
resource aiSearch 'Microsoft.Search/searchServices@2023-11-01' = {
  name: toLower('srch-${baseName}-${uniqueSuffix}')
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
  }
}

// Role assignment for OpenAI - Cognitive Services OpenAI User
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: openAI
  name: guid(openAI.id, managedIdentityPrincipalId, 'Cognitive Services OpenAI User')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Role assignment for AI Search - Search Index Data Reader
resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: aiSearch
  name: guid(aiSearch.id, managedIdentityPrincipalId, 'Search Index Data Reader')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '1407120a-92aa-4202-b7e9-c0e197c71c8f')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

@description('The endpoint of the Azure OpenAI service')
output openAIEndpoint string = openAI.properties.endpoint

@description('The name of the deployed model')
output openAIModelName string = openAIDeployment.name

@description('The name of the Azure OpenAI resource')
output openAIName string = openAI.name

@description('The endpoint of the Azure AI Search service')
output searchEndpoint string = 'https://${aiSearch.name}.search.windows.net'

@description('The name of the Azure AI Search resource')
output searchName string = aiSearch.name
