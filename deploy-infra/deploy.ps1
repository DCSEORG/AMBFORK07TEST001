#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys Azure infrastructure for the Expense Management application.

.DESCRIPTION
    This script automates the deployment of all Azure resources including:
    - Managed Identity
    - App Service and App Service Plan
    - Azure SQL Database with Entra ID authentication
    - Log Analytics Workspace and Application Insights
    - Optional: Azure OpenAI and AI Search (with -DeployGenAI switch)

.PARAMETER ResourceGroup
    Name of the Azure resource group (required).

.PARAMETER Location
    Azure region for deployment (required).

.PARAMETER BaseName
    Base name for all resources (default: "expensemgmt").

.PARAMETER DeployGenAI
    Switch to deploy Azure OpenAI and AI Search resources.

.PARAMETER SkipDatabaseSetup
    Switch to skip database schema import and setup (useful for redeployments).

.EXAMPLE
    .\deploy.ps1 -ResourceGroup "rg-expensemgmt-20250101" -Location "uksouth"

.EXAMPLE
    .\deploy.ps1 -ResourceGroup "rg-expensemgmt-20250101" -Location "uksouth" -DeployGenAI
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$true)]
    [string]$Location,
    
    [Parameter(Mandatory=$false)]
    [string]$BaseName = "expensemgmt",
    
    [Parameter(Mandatory=$false)]
    [switch]$DeployGenAI,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipDatabaseSetup
)

$ErrorActionPreference = "Stop"

# Detect CI/CD environment
$IsCI = $env:GITHUB_ACTIONS -eq "true" -or $env:TF_BUILD -eq "true" -or $env:CI -eq "true"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Expense Management Infrastructure Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check PowerShell version
if ($PSVersionTable.PSVersion.Major -lt 7) {
    Write-Warning "You are running PowerShell $($PSVersionTable.PSVersion). PowerShell 7+ is recommended."
    Write-Warning "Download from: https://github.com/PowerShell/PowerShell/releases"
}

# Check Azure CLI
Write-Host "Checking prerequisites..." -ForegroundColor Yellow
try {
    $azVersion = az version --output json 2>$null | ConvertFrom-Json
    Write-Host "✓ Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Error "Azure CLI is not installed. Install from: https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
}

# Check login status
Write-Host "Checking Azure login status..." -ForegroundColor Yellow
try {
    $account = az account show --output json 2>$null | ConvertFrom-Json
    Write-Host "✓ Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "✓ Subscription: $($account.name) ($($account.id))" -ForegroundColor Green
} catch {
    Write-Error "Not logged in to Azure. Run 'az login' first."
    exit 1
}

# Get admin credentials based on environment
Write-Host ""
Write-Host "Retrieving administrator credentials..." -ForegroundColor Yellow

if ($IsCI) {
    Write-Host "Running in CI/CD mode" -ForegroundColor Cyan
    
    # In CI/CD, use service principal credentials
    $servicePrincipalClientId = $env:AZURE_CLIENT_ID
    if ([string]::IsNullOrEmpty($servicePrincipalClientId)) {
        Write-Error "AZURE_CLIENT_ID environment variable not set"
        exit 1
    }
    
    # Get service principal details
    $spDetails = az ad sp show --id $servicePrincipalClientId --output json | ConvertFrom-Json
    $adminObjectId = $spDetails.id
    $adminLogin = $spDetails.displayName
    $adminPrincipalType = "Application"
    
    Write-Host "✓ Service Principal: $adminLogin" -ForegroundColor Green
    Write-Host "✓ Object ID: $adminObjectId" -ForegroundColor Green
} else {
    Write-Host "Running in interactive mode" -ForegroundColor Cyan
    
    # In interactive mode, use current user
    $user = az ad signed-in-user show --output json | ConvertFrom-Json
    $adminObjectId = $user.id
    $adminLogin = $user.userPrincipalName
    $adminPrincipalType = "User"
    
    Write-Host "✓ User: $adminLogin" -ForegroundColor Green
    Write-Host "✓ Object ID: $adminObjectId" -ForegroundColor Green
}

# Create resource group
Write-Host ""
Write-Host "Creating resource group: $ResourceGroup" -ForegroundColor Yellow
az group create --name $ResourceGroup --location $Location --output none
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create resource group"
    exit 1
}
Write-Host "✓ Resource group created" -ForegroundColor Green

# Deploy Bicep template
Write-Host ""
Write-Host "Deploying Azure resources..." -ForegroundColor Yellow
Write-Host "This may take 5-10 minutes..." -ForegroundColor Gray

$deploymentName = "infra-deployment-$(Get-Date -Format 'yyyyMMddHHmmss')"

$deploymentOutput = az deployment group create `
    --resource-group $ResourceGroup `
    --name $deploymentName `
    --template-file "$PSScriptRoot/main.bicep" `
    --parameters location=$Location `
                 baseName=$BaseName `
                 deployGenAI=$($DeployGenAI.IsPresent) `
                 adminObjectId=$adminObjectId `
                 adminLogin=$adminLogin `
                 adminPrincipalType=$adminPrincipalType `
    --output json

if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment failed"
    exit 1
}

$deployment = $deploymentOutput | ConvertFrom-Json
$outputs = $deployment.properties.outputs

Write-Host "✓ Infrastructure deployed successfully" -ForegroundColor Green

# Extract output values
$webAppName = $outputs.webAppName.value
$webAppHostName = $outputs.webAppHostName.value
$sqlServerFqdn = $outputs.sqlServerFqdn.value
$sqlServerName = $outputs.sqlServerName.value
$sqlDatabaseName = $outputs.sqlDatabaseName.value
$managedIdentityClientId = $outputs.managedIdentityClientId.value
$managedIdentityName = $outputs.managedIdentityName.value
$appInsightsConnectionString = $outputs.appInsightsConnectionString.value

Write-Host ""
Write-Host "Deployed Resources:" -ForegroundColor Cyan
Write-Host "  Web App: $webAppName" -ForegroundColor White
Write-Host "  SQL Server: $sqlServerName" -ForegroundColor White
Write-Host "  SQL Database: $sqlDatabaseName" -ForegroundColor White
Write-Host "  Managed Identity: $managedIdentityName" -ForegroundColor White

# Database setup
if (-not $SkipDatabaseSetup) {
    Write-Host ""
    Write-Host "Setting up database..." -ForegroundColor Yellow
    
    # Wait for SQL Server to be ready
    Write-Host "Waiting for SQL Server to be ready..." -ForegroundColor Gray
    Start-Sleep -Seconds 30
    
    # Add current IP to firewall (for interactive mode)
    if (-not $IsCI) {
        Write-Host "Adding your IP to SQL Server firewall..." -ForegroundColor Gray
        try {
            $myIp = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing).Content
            az sql server firewall-rule create `
                --resource-group $ResourceGroup `
                --server $sqlServerName `
                --name "ClientIp" `
                --start-ip-address $myIp `
                --end-ip-address $myIp `
                --output none
            Write-Host "✓ Firewall rule added" -ForegroundColor Green
        } catch {
            Write-Warning "Could not add firewall rule. Continuing..."
        }
    }
    
    # Check sqlcmd availability
    Write-Host "Checking sqlcmd availability..." -ForegroundColor Gray
    $sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if (-not $sqlcmdPath) {
        Write-Warning "sqlcmd not found. Install with: winget install sqlcmd"
        Write-Warning "Skipping database setup. Run this script again after installing sqlcmd."
    } else {
        # Determine authentication method
        $authMethod = if ($IsCI) { "ActiveDirectoryAzCli" } else { "ActiveDirectoryDefault" }
        
        # Import database schema
        Write-Host "Importing database schema..." -ForegroundColor Gray
        $schemaFile = Join-Path $PSScriptRoot ".." "Database-Schema" "database_schema.sql"
        
        if (Test-Path $schemaFile) {
            try {
                sqlcmd -S $sqlServerFqdn -d $sqlDatabaseName "--authentication-method=$authMethod" -i $schemaFile
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✓ Database schema imported" -ForegroundColor Green
                } else {
                    Write-Warning "Schema import had errors. Check output above."
                }
            } catch {
                Write-Warning "Failed to import schema: $_"
            }
        } else {
            Write-Warning "Schema file not found: $schemaFile"
        }
        
        # Create managed identity database user (SID-based)
        Write-Host "Creating managed identity database user..." -ForegroundColor Gray
        try {
            # Convert Client ID to SID hex format
            $guidBytes = [System.Guid]::Parse($managedIdentityClientId).ToByteArray()
            $sidHex = "0x" + [System.BitConverter]::ToString($guidBytes).Replace("-", "")
            
            $createUserSql = @"
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = '$managedIdentityName')
    DROP USER [$managedIdentityName];

CREATE USER [$managedIdentityName] WITH SID = $sidHex, TYPE = E;

ALTER ROLE db_datareader ADD MEMBER [$managedIdentityName];
ALTER ROLE db_datawriter ADD MEMBER [$managedIdentityName];
GRANT EXECUTE TO [$managedIdentityName];
"@
            
            $createUserSql | sqlcmd -S $sqlServerFqdn -d $sqlDatabaseName "--authentication-method=$authMethod"
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Managed identity user created" -ForegroundColor Green
            } else {
                Write-Warning "Failed to create managed identity user"
            }
        } catch {
            Write-Warning "Failed to create managed identity user: $_"
        }
        
        # Import stored procedures
        Write-Host "Importing stored procedures..." -ForegroundColor Gray
        $storedProcFile = Join-Path $PSScriptRoot ".." "stored-procedures.sql"
        
        if (Test-Path $storedProcFile) {
            try {
                sqlcmd -S $sqlServerFqdn -d $sqlDatabaseName "--authentication-method=$authMethod" -i $storedProcFile
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✓ Stored procedures imported" -ForegroundColor Green
                } else {
                    Write-Warning "Stored procedure import had errors. Check output above."
                }
            } catch {
                Write-Warning "Failed to import stored procedures: $_"
            }
        } else {
            Write-Warning "Stored procedures file not found: $storedProcFile"
        }
    }
}

# Configure App Service settings
Write-Host ""
Write-Host "Configuring App Service settings..." -ForegroundColor Yellow

$connectionString = "Server=tcp:$sqlServerFqdn,1433;Initial Catalog=$sqlDatabaseName;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Managed Identity;User Id=$managedIdentityClientId;"

az webapp config connection-string set `
    --resource-group $ResourceGroup `
    --name $webAppName `
    --connection-string-type SQLAzure `
    --settings "DefaultConnection=$connectionString" `
    --output none

az webapp config appsettings set `
    --resource-group $ResourceGroup `
    --name $webAppName `
    --settings "AZURE_CLIENT_ID=$managedIdentityClientId" `
               "ManagedIdentityClientId=$managedIdentityClientId" `
    --output none

Write-Host "✓ App Service settings configured" -ForegroundColor Green

# Configure GenAI settings if deployed
if ($DeployGenAI) {
    Write-Host ""
    Write-Host "Configuring GenAI settings..." -ForegroundColor Yellow
    
    $openAIEndpoint = $outputs.openAIEndpoint.value
    $openAIModelName = $outputs.openAIModelName.value
    $searchEndpoint = $outputs.searchEndpoint.value
    
    az webapp config appsettings set `
        --resource-group $ResourceGroup `
        --name $webAppName `
        --settings "OpenAI__Endpoint=$openAIEndpoint" `
                   "OpenAI__DeploymentName=$openAIModelName" `
                   "AzureSearch__Endpoint=$searchEndpoint" `
        --output none
    
    Write-Host "✓ GenAI settings configured" -ForegroundColor Green
    Write-Host "  OpenAI Endpoint: $openAIEndpoint" -ForegroundColor White
    Write-Host "  Model: $openAIModelName" -ForegroundColor White
}

# Save deployment context
Write-Host ""
Write-Host "Saving deployment context..." -ForegroundColor Yellow

$contextFile = Join-Path $PSScriptRoot ".." ".deployment-context.json"
$context = @{
    resourceGroup = $ResourceGroup
    location = $Location
    webAppName = $webAppName
    webAppHostName = $webAppHostName
    sqlServerFqdn = $sqlServerFqdn
    managedIdentityClientId = $managedIdentityClientId
    deployedGenAI = $DeployGenAI.IsPresent
}

if ($DeployGenAI) {
    $context.openAIEndpoint = $outputs.openAIEndpoint.value
    $context.openAIModelName = $outputs.openAIModelName.value
}

$context | ConvertTo-Json | Set-Content -Path $contextFile
Write-Host "✓ Deployment context saved to: $contextFile" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Deploy application code:" -ForegroundColor White
Write-Host "   .\deploy-app\deploy.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Access your application:" -ForegroundColor White
Write-Host "   https://$webAppHostName/Index" -ForegroundColor Gray
Write-Host ""
Write-Host "3. View API documentation:" -ForegroundColor White
Write-Host "   https://$webAppHostName/swagger" -ForegroundColor Gray
Write-Host ""
