# Application Deployment

This directory contains scripts for deploying the Expense Management application code to Azure App Service.

## Prerequisites

- **.NET 8 SDK**: [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Azure CLI**: Already installed (same as infrastructure deployment)
- **Completed Infrastructure Deployment**: Run `deploy-infra/deploy.ps1` first

## Quick Start

After deploying infrastructure, simply run:

```powershell
.\deploy-app\deploy.ps1
```

The script automatically reads the deployment context from `.deployment-context.json` (created by the infrastructure deployment), so you don't need to specify any parameters.

## Manual Deployment

If you need to specify parameters manually:

```powershell
.\deploy-app\deploy.ps1 -ResourceGroup "rg-expensemgmt-prod" -WebAppName "app-expensemgmt-xyz123"
```

## Script Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `ResourceGroup` | No* | From context | Azure resource group name |
| `WebAppName` | No* | From context | Azure App Service name |
| `SkipBuild` | No | false | Skip building the application (use existing package) |
| `ConfigureSettings` | No | false | Configure App Service settings after deployment |

*Required only if `.deployment-context.json` doesn't exist

## What the Script Does

1. **Loads Context**: Reads deployment details from `.deployment-context.json`
2. **Validates Prerequisites**: Checks Azure CLI and login status
3. **Restores Dependencies**: Runs `dotnet restore`
4. **Builds Application**: Compiles the .NET 8 application in Release mode
5. **Creates Package**: Packages the application as a ZIP file
6. **Deploys to Azure**: Uploads and deploys using `az webapp deploy`
7. **Cleans Up**: Removes temporary files
8. **Displays URLs**: Shows the application URLs

## Deployment Package Structure

The deployment creates a ZIP file with the following structure:

```
deploy.zip
├── ExpenseManagement.dll
├── ExpenseManagement.deps.json
├── ExpenseManagement.runtimeconfig.json
├── appsettings.json
├── web.config
└── ... (other dependencies)
```

**Important**: The DLL files must be at the root of the ZIP, not in a subdirectory. Azure App Service expects this structure.

## Application URLs

After deployment, your application will be available at:

- **Main Application**: `https://{webappname}.azurewebsites.net/Index`
- **API Documentation**: `https://{webappname}.azurewebsites.net/swagger`
- **AI Chat** (if GenAI deployed): `https://{webappname}.azurewebsites.net/Chat`

Note: The root URL (`/`) doesn't have a page. Use `/Index` for the main application.

## Redeployment

For subsequent deployments (after making code changes):

```powershell
# Quick redeploy with rebuild
.\deploy-app\deploy.ps1

# Redeploy without rebuilding (if you have a recent build)
.\deploy-app\deploy.ps1 -SkipBuild
```

## Configuration

### Connection Strings

The application connection string is automatically configured during infrastructure deployment. It uses:

- **Managed Identity authentication** (no passwords stored)
- **Format**: `Server=tcp:{server},1433;Initial Catalog=Northwind;Authentication=Active Directory Managed Identity;User Id={clientId}`

### Application Settings

The following settings are configured automatically:

- `AZURE_CLIENT_ID`: Managed identity client ID (for DefaultAzureCredential)
- `ManagedIdentityClientId`: Same as above (used by chat service)
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Application Insights connection
- `OpenAI__Endpoint`: Azure OpenAI endpoint (if GenAI deployed)
- `OpenAI__DeploymentName`: Model deployment name (if GenAI deployed)

### Local Development

For local development, create `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=Northwind;Authentication=Active Directory Default;"
  }
}
```

Use `Authentication=Active Directory Default` for local development. This authenticates using your Azure CLI credentials (`az login`).

## Troubleshooting

### "Project file not found"

Ensure you're running the script from the repository root or the `deploy-app` directory.

### "Failed to publish application"

Check that:
- .NET 8 SDK is installed: `dotnet --version`
- All NuGet packages are accessible
- You have internet connectivity

### "Deployment failed"

Common causes:
- Azure CLI not logged in: Run `az login`
- Insufficient permissions: Ensure you have Contributor role
- App Service not ready: Wait a few minutes after infrastructure deployment

### "Application shows errors after deployment"

Check:
1. **Application Insights Logs**: View in Azure Portal
2. **App Service Logs**: Enable logging in Azure Portal > App Service > Monitoring > App Service logs
3. **Connection String**: Verify it's configured correctly with managed identity

Common issues:
- "Unable to load the proper Managed Identity": AZURE_CLIENT_ID not set
- "Login failed for user": Managed identity doesn't have database permissions
- Database connection errors: Check that the SQL Server firewall allows Azure services

### Startup Time

The App Service may take 30-60 seconds to start after deployment. If you get a "503 Service Unavailable" error, wait a minute and refresh.

## Build Configuration

The application is built with:

- **Target Framework**: .NET 8.0
- **Configuration**: Release
- **Output**: Self-contained publish (includes runtime)
- **Optimization**: Enabled

## Continuous Deployment

For automated deployment via GitHub Actions, see the workflow at `.github/workflows/deploy.yml`.

The CI/CD pipeline automatically:
1. Builds the application
2. Runs tests (if present)
3. Deploys to Azure
4. Waits for App Service to stabilize

## Next Steps

After deployment:

1. **Test the Application**: Visit the main URL and verify functionality
2. **Check API Documentation**: Open `/swagger` to see all available endpoints
3. **Monitor Performance**: Use Application Insights in Azure Portal
4. **Test AI Chat**: If GenAI deployed, try the chat interface at `/Chat`

## Support

For deployment issues:
- Check Azure Portal > App Service > Deployment Center for deployment history
- View logs in Application Insights
- Review diagnostic logs in App Service

For application issues:
- Check Application Insights for exceptions and errors
- Enable detailed logging in App Service
- Test API endpoints using Swagger UI
