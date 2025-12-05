# Infrastructure Deployment

This directory contains all the infrastructure-as-code (IaC) for deploying the Expense Management application to Azure.

## Prerequisites

- **Azure CLI**: [Install from here](https://docs.microsoft.com/cli/azure/install-azure-cli)
- **PowerShell 7+**: [Download from here](https://github.com/PowerShell/PowerShell/releases) (recommended)
- **go-sqlcmd**: Install with `winget install sqlcmd` on Windows
- **Azure Subscription**: With appropriate permissions to create resources

## Quick Start

1. **Login to Azure**:
   ```powershell
   az login
   ```

2. **Deploy Infrastructure**:
   ```powershell
   .\deploy-infra\deploy.ps1 -ResourceGroup "rg-expensemgmt-20250105" -Location "uksouth"
   ```

3. **Deploy with GenAI** (optional):
   ```powershell
   .\deploy-infra\deploy.ps1 -ResourceGroup "rg-expensemgmt-20250105" -Location "uksouth" -DeployGenAI
   ```

## What Gets Deployed

### Base Infrastructure (always deployed)

- **Managed Identity**: User-assigned managed identity for secure authentication
- **App Service**: Standard S1 tier (Linux, .NET 8)
- **Azure SQL Database**: Basic tier with Entra ID-only authentication
- **Log Analytics Workspace**: For centralized logging
- **Application Insights**: For application telemetry and monitoring

### Optional GenAI Resources (with -DeployGenAI switch)

- **Azure OpenAI**: GPT-4o model (deployed in Sweden Central for better quota)
- **Azure AI Search**: Basic tier for RAG (Retrieval-Augmented Generation)

## Deployment Script Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `ResourceGroup` | Yes | - | Name of the Azure resource group |
| `Location` | Yes | - | Azure region (e.g., "uksouth") |
| `BaseName` | No | "expensemgmt" | Base name for all resources |
| `DeployGenAI` | No | false | Deploy Azure OpenAI and AI Search |
| `SkipDatabaseSetup` | No | false | Skip schema import and database setup |

## Resource Naming Convention

All resources use lowercase names with a consistent pattern:

- Managed Identity: `mid-{basename}-{uniqueid}`
- App Service: `app-{basename}-{uniqueid}`
- App Service Plan: `asp-{basename}-{uniqueid}`
- SQL Server: `sql-{basename}-{uniqueid}`
- Log Analytics: `log-{basename}-{uniqueid}`
- Application Insights: `appi-{basename}-{uniqueid}`
- Azure OpenAI: `oai-{basename}-{uniqueid}`
- Azure AI Search: `srch-{basename}-{uniqueid}`

The `{uniqueid}` is generated using `uniqueString(resourceGroup().id)` to ensure uniqueness.

## What the Script Does

1. **Prerequisites Check**: Verifies Azure CLI and login status
2. **Credential Retrieval**: Gets current user or service principal details
3. **Resource Group Creation**: Creates the resource group if needed
4. **Bicep Deployment**: Deploys all infrastructure using the main.bicep template
5. **Database Setup**:
   - Waits for SQL Server to be ready
   - Adds firewall rules (interactive mode only)
   - Imports database schema
   - Creates managed identity database user
   - Imports stored procedures
6. **App Service Configuration**: Sets connection strings and managed identity settings
7. **GenAI Configuration** (if applicable): Configures OpenAI and Search endpoints
8. **Context File**: Saves deployment details to `.deployment-context.json`

## Important Notes

### Resource Group Naming

**Always use a unique resource group name** (include date/time suffix like `rg-expensemgmt-20250105`).

Reusing resource groups with partially deployed resources can cause ARM caching issues, particularly with Log Analytics Workspace references.

If a deployment fails, delete the resource group and try again with a new name:
```powershell
az group delete --name "rg-expensemgmt-20250105" --yes
```

### SQL Authentication

The deployment uses **Entra ID-only authentication** for Azure SQL:
- No SQL username/password authentication
- Complies with security governance policies
- Uses managed identity for application access

### sqlcmd Issues

If you get errors about unrecognized sqlcmd arguments:
- You might be using the legacy ODBC sqlcmd instead of go-sqlcmd
- Restart VS Code or run from a standalone PowerShell terminal
- Ensure go-sqlcmd is installed: `winget install sqlcmd`

## Bicep Template Structure

```
deploy-infra/
├── main.bicep              # Main orchestration template
├── main.bicepparam         # Parameter file
├── modules/
│   ├── managed-identity.bicep   # User-assigned managed identity
│   ├── app-service.bicep        # App Service and Plan
│   ├── azure-sql.bicep          # SQL Server and Database
│   ├── monitoring.bicep         # Log Analytics and App Insights
│   └── genai.bicep              # Azure OpenAI and AI Search
├── deploy.ps1              # Deployment automation script
└── README.md               # This file
```

## Deployment Context File

After deployment, a `.deployment-context.json` file is created at the repository root. This file contains:

- Resource group name
- Web app name and hostname
- SQL server FQDN
- Managed identity client ID
- GenAI endpoints (if deployed)

The application deployment script (`deploy-app/deploy.ps1`) reads this file automatically, so you don't need to specify any parameters.

## CI/CD Support

This deployment script supports both:
- **Interactive deployment**: Run locally with `az login`
- **CI/CD deployment**: GitHub Actions with OIDC authentication

See [../.github/CICD-SETUP.md](../.github/CICD-SETUP.md) for CI/CD setup instructions.

## Troubleshooting

### "Could not retrieve the Log Analytics workspace from ARM"

This usually indicates ARM caching issues. Use a fresh resource group name.

### "Login failed for user"

The managed identity database user might not have been created correctly. Check:
1. The managed identity exists
2. The firewall allows your IP
3. You're using the correct authentication method in sqlcmd

### "Unable to load the proper Managed Identity"

The `AZURE_CLIENT_ID` environment variable is not set. The deployment script handles this automatically.

## Manual Deployment (Advanced)

If you prefer to deploy manually without the PowerShell script:

```bash
# Login
az login

# Create resource group
az group create --name "rg-expensemgmt" --location "uksouth"

# Get your Object ID and UPN
$user = az ad signed-in-user show | ConvertFrom-Json

# Deploy
az deployment group create \
  --resource-group "rg-expensemgmt" \
  --template-file ./deploy-infra/main.bicep \
  --parameters location=uksouth \
               baseName=expensemgmt \
               deployGenAI=false \
               adminObjectId=$user.id \
               adminLogin=$user.userPrincipalName \
               adminPrincipalType=User
```

## Next Steps

After infrastructure deployment:

1. **Deploy application code**: `.\deploy-app\deploy.ps1`
2. **Access the application**: Visit the URL shown in the deployment output
3. **View logs**: Use Azure Portal to access Application Insights and Log Analytics
