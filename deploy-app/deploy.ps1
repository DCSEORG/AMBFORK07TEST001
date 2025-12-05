#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys the Expense Management application to Azure App Service.

.DESCRIPTION
    This script builds the .NET application and deploys it to Azure App Service.
    It automatically reads deployment context from .deployment-context.json if available.

.PARAMETER ResourceGroup
    Name of the Azure resource group (optional if context file exists).

.PARAMETER WebAppName
    Name of the Azure App Service (optional if context file exists).

.PARAMETER SkipBuild
    Skip the build and publish step (useful for redeployments).

.PARAMETER ConfigureSettings
    Configure App Service settings after deployment.

.EXAMPLE
    .\deploy.ps1
    (Uses context file from infrastructure deployment)

.EXAMPLE
    .\deploy.ps1 -ResourceGroup "rg-expensemgmt-prod" -WebAppName "app-expensemgmt-xyz"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$false)]
    [string]$WebAppName,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild,
    
    [Parameter(Mandatory=$false)]
    [switch]$ConfigureSettings
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Expense Management Application Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Try to load deployment context
$contextFile = Join-Path $PSScriptRoot ".." ".deployment-context.json"
$context = $null

if (Test-Path $contextFile) {
    Write-Host "Loading deployment context from file..." -ForegroundColor Yellow
    $context = Get-Content $contextFile | ConvertFrom-Json
    
    if ([string]::IsNullOrEmpty($ResourceGroup)) {
        $ResourceGroup = $context.resourceGroup
        Write-Host "  Resource Group: $ResourceGroup (from context)" -ForegroundColor Gray
    }
    
    if ([string]::IsNullOrEmpty($WebAppName)) {
        $WebAppName = $context.webAppName
        Write-Host "  Web App: $WebAppName (from context)" -ForegroundColor Gray
    }
}

# Validate required parameters
if ([string]::IsNullOrEmpty($ResourceGroup) -or [string]::IsNullOrEmpty($WebAppName)) {
    Write-Error "ResourceGroup and WebAppName are required. Either provide them as parameters or ensure .deployment-context.json exists."
    exit 1
}

# Check Azure CLI
Write-Host ""
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
} catch {
    Write-Error "Not logged in to Azure. Run 'az login' first."
    exit 1
}

# Build and publish application
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Building application..." -ForegroundColor Yellow
    
    $projectPath = Join-Path $PSScriptRoot ".." "src" "ExpenseManagement" "ExpenseManagement.csproj"
    $publishPath = Join-Path $PSScriptRoot ".." "src" "ExpenseManagement" "publish"
    
    if (-not (Test-Path $projectPath)) {
        Write-Error "Project file not found: $projectPath"
        exit 1
    }
    
    # Clean previous publish
    if (Test-Path $publishPath) {
        Remove-Item -Path $publishPath -Recurse -Force
    }
    
    # Restore dependencies
    Write-Host "Restoring dependencies..." -ForegroundColor Gray
    dotnet restore $projectPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to restore dependencies"
        exit 1
    }
    
    # Build and publish
    Write-Host "Publishing application..." -ForegroundColor Gray
    dotnet publish $projectPath -c Release -o $publishPath --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to publish application"
        exit 1
    }
    
    Write-Host "✓ Application built successfully" -ForegroundColor Green
    
    # Create deployment zip
    Write-Host ""
    Write-Host "Creating deployment package..." -ForegroundColor Yellow
    
    $zipPath = Join-Path $PSScriptRoot "deploy.zip"
    if (Test-Path $zipPath) {
        Remove-Item -Path $zipPath -Force
    }
    
    # Create zip with DLL files at root level (Azure expects this structure)
    Push-Location $publishPath
    try {
        if ($IsWindows -or $PSVersionTable.PSVersion.Major -le 5) {
            Compress-Archive -Path * -DestinationPath $zipPath
        } else {
            # Use zip command on Linux/Mac
            zip -r $zipPath * | Out-Null
        }
    } finally {
        Pop-Location
    }
    
    Write-Host "✓ Deployment package created" -ForegroundColor Green
} else {
    Write-Host "Skipping build (using existing deployment package)" -ForegroundColor Yellow
}

# Deploy to Azure
Write-Host ""
Write-Host "Deploying to Azure App Service..." -ForegroundColor Yellow
Write-Host "This may take a few minutes..." -ForegroundColor Gray

$zipPath = Join-Path $PSScriptRoot "deploy.zip"

if (-not (Test-Path $zipPath)) {
    Write-Error "Deployment package not found: $zipPath. Run without -SkipBuild."
    exit 1
}

az webapp deploy `
    --resource-group $ResourceGroup `
    --name $WebAppName `
    --src-path $zipPath `
    --type zip `
    --clean true `
    --restart true `
    --output none

if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment failed"
    exit 1
}

Write-Host "✓ Application deployed successfully" -ForegroundColor Green

# Clean up deployment package
Write-Host ""
Write-Host "Cleaning up..." -ForegroundColor Yellow
Remove-Item -Path $zipPath -Force
Write-Host "✓ Cleanup complete" -ForegroundColor Green

# Configure settings if requested
if ($ConfigureSettings -and $context) {
    Write-Host ""
    Write-Host "Configuring App Service settings..." -ForegroundColor Yellow
    
    $connectionString = "Server=tcp:$($context.sqlServerFqdn),1433;Initial Catalog=Northwind;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Managed Identity;User Id=$($context.managedIdentityClientId);"
    
    az webapp config connection-string set `
        --resource-group $ResourceGroup `
        --name $WebAppName `
        --connection-string-type SQLAzure `
        --settings "DefaultConnection=$connectionString" `
        --output none
    
    az webapp config appsettings set `
        --resource-group $ResourceGroup `
        --name $WebAppName `
        --settings "AZURE_CLIENT_ID=$($context.managedIdentityClientId)" `
                   "ManagedIdentityClientId=$($context.managedIdentityClientId)" `
        --output none
    
    Write-Host "✓ App Service settings configured" -ForegroundColor Green
}

# Display URLs
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($context) {
    $hostName = $context.webAppHostName
} else {
    $hostName = "$WebAppName.azurewebsites.net"
}

Write-Host "Application URLs:" -ForegroundColor Yellow
Write-Host "  Main Application: https://$hostName/Index" -ForegroundColor White
Write-Host "  API Documentation: https://$hostName/swagger" -ForegroundColor White
Write-Host "  AI Chat: https://$hostName/Chat" -ForegroundColor White
Write-Host ""
Write-Host "Note: It may take a minute for the application to start." -ForegroundColor Gray
Write-Host ""
