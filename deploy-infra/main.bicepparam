using './main.bicep'

// These parameters will be overridden by the deployment script
param location = 'uksouth'
param baseName = 'expensemgmt'
param deployGenAI = false
param adminObjectId = ''
param adminLogin = ''
param adminPrincipalType = 'User'
