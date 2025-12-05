# GitHub Actions CI/CD Setup Guide

This guide walks you through setting up automated deployment using GitHub Actions with OIDC (OpenID Connect) authentication.

## Overview

The deployment uses **federated identity credentials** (OIDC) instead of secrets, which is more secure and follows Azure best practices.

## Prerequisites

- Azure subscription with appropriate permissions
- Repository admin access to configure secrets and environments
- PowerShell 7+ for running setup commands

## Step 1: Create Azure Service Principal with OIDC

Run these commands in PowerShell to create a service principal with federated credentials:

```powershell
# Set your variables
$subscriptionId = "YOUR_SUBSCRIPTION_ID"
$resourceGroup = "rg-expensemgmt-cicd"  # This will be created for CI/CD
$appName = "sp-expensemgmt-cicd"
$githubOrg = "YOUR_GITHUB_ORG"
$githubRepo = "YOUR_GITHUB_REPO"

# Login to Azure
az login

# Set subscription
az account set --subscription $subscriptionId

# Create service principal
$sp = az ad sp create-for-rbac --name $appName --role Contributor --scopes /subscriptions/$subscriptionId --sdk-auth | ConvertFrom-Json

# Get the service principal's Object ID
$spObjectId = az ad sp show --id $sp.clientId --query id -o tsv

Write-Host "Service Principal Created:" -ForegroundColor Green
Write-Host "  Client ID: $($sp.clientId)"
Write-Host "  Tenant ID: $($sp.tenantId)"
Write-Host "  Object ID: $spObjectId"
```

## Step 2: Assign Required Roles

The service principal needs **TWO** roles at the subscription level:

```powershell
# Assign Contributor role (for creating/managing resources)
az role assignment create `
  --assignee $sp.clientId `
  --role "Contributor" `
  --scope /subscriptions/$subscriptionId

# Assign User Access Administrator role (for creating role assignments)
az role assignment create `
  --assignee $sp.clientId `
  --role "User Access Administrator" `
  --scope /subscriptions/$subscriptionId

Write-Host "✓ Roles assigned successfully" -ForegroundColor Green
```

### Why Two Roles?

| Role | Purpose |
|------|---------|
| **Contributor** | Create and manage Azure resources (App Service, SQL, OpenAI, etc.) |
| **User Access Administrator** | Create role assignments (required when Bicep assigns Managed Identity access to resources) |

Without **User Access Administrator**, deployments that include role assignments in Bicep will fail with:
> "The client does not have permission to perform action 'Microsoft.Authorization/roleAssignments/write'"

## Step 3: Create Federated Credentials

Create federated credentials for your GitHub repository:

```powershell
# For main branch deployments
az ad app federated-credential create `
  --id $sp.appId `
  --parameters @"
{
  \"name\": \"$appName-main\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$githubOrg/$githubRepo:ref:refs/heads/main\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}
"@

# For pull request deployments (optional)
az ad app federated-credential create `
  --id $sp.appId `
  --parameters @"
{
  \"name\": \"$appName-pr\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$githubOrg/$githubRepo:pull_request\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}
"@

# For environment-based deployments
az ad app federated-credential create `
  --id $sp.appId `
  --parameters @"
{
  \"name\": \"$appName-production\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$githubOrg/$githubRepo:environment:production\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}
"@

Write-Host "✓ Federated credentials created" -ForegroundColor Green
```

## Step 4: Configure GitHub Repository

### Create GitHub Environment

1. Go to your repository on GitHub
2. Navigate to **Settings** > **Environments**
3. Click **New environment**
4. Name it `production`
5. Click **Configure environment**

### Add Repository Variables

In your GitHub repository, go to **Settings** > **Secrets and variables** > **Actions** > **Variables** tab:

Click **New repository variable** and add these three variables:

| Name | Value | Description |
|------|-------|-------------|
| `AZURE_CLIENT_ID` | `<your-sp-client-id>` | The Client ID from Step 1 |
| `AZURE_TENANT_ID` | `<your-tenant-id>` | Your Azure tenant ID |
| `AZURE_SUBSCRIPTION_ID` | `<your-subscription-id>` | Your Azure subscription ID |

**Important**: These should be **Variables**, NOT Secrets, as they're not sensitive and need to be accessible in the workflow.

## Step 5: Test the Workflow

### Manual Trigger

1. Go to **Actions** tab in your GitHub repository
2. Select the **Deploy to Azure** workflow
3. Click **Run workflow**
4. Choose whether to deploy GenAI resources
5. Click **Run workflow**

### Automatic Trigger

Push to the `main` branch to trigger automatic deployment:

```bash
git add .
git commit -m "Test CI/CD deployment"
git push origin main
```

## Troubleshooting

### "Login failed" or "Could not authenticate"

**Cause**: Federated credentials not set up correctly or token audience mismatch.

**Solution**: 
- Verify the federated credential subject matches your repository exactly
- Ensure the credential is for the correct branch/environment
- Check that OIDC is enabled in your workflow (`permissions: id-token: write`)

### "The client does not have permission to perform action 'Microsoft.Authorization/roleAssignments/write'"

**Cause**: Service principal doesn't have the **User Access Administrator** role.

**Solution**: Follow Step 2 to assign both required roles.

### "sqlcmd: command not found"

**Cause**: The sqlcmd installation step failed or was skipped.

**Solution**: Check the workflow logs for the "Install sqlcmd" step. The workflow downloads sqlcmd from GitHub releases.

### "Unable to load the proper Managed Identity"

**Cause**: The `AZURE_CLIENT_ID` environment variable is not being set correctly in CI/CD.

**Solution**: The deployment script automatically handles this. Ensure you're using the latest version of `deploy.ps1`.

## CI/CD vs Local Deployment Differences

### Authentication Method

| Mode | Admin Type | sqlcmd Auth | User Detection |
|------|-----------|-------------|----------------|
| **Local** | `User` | `ActiveDirectoryDefault` | `az ad signed-in-user show` |
| **CI/CD** | `Application` | `ActiveDirectoryAzCli` | Uses `$env:AZURE_CLIENT_ID` |

### Why Different sqlcmd Authentication?

In CI/CD with OIDC:
- `ActiveDirectoryDefault` tries `EnvironmentCredential` first
- It sees `AZURE_CLIENT_ID` and `AZURE_TENANT_ID` but no `AZURE_CLIENT_SECRET`
- This causes "Identity not found" errors
- `ActiveDirectoryAzCli` uses the Azure CLI token directly

## Security Best Practices

1. **Use OIDC**: Never store long-lived credentials as GitHub secrets
2. **Limit Scope**: Create service principals with minimal required permissions
3. **Environment Protection**: Use GitHub environment protection rules
4. **Review Logs**: Regularly review deployment logs for anomalies
5. **Rotate Credentials**: Even with OIDC, rotate service principals periodically

## GitHub Environment Protection (Recommended)

Add protection rules to your `production` environment:

1. **Required Reviewers**: Require manual approval before deployment
2. **Wait Timer**: Add a delay before deployment
3. **Deployment Branches**: Restrict to specific branches (e.g., `main`)

## Advanced: Multiple Environments

To deploy to multiple environments (dev, staging, production):

1. Create separate Azure service principals for each environment
2. Create separate GitHub environments (dev, staging, production)
3. Configure environment-specific variables in each GitHub environment
4. Create federated credentials for each environment
5. Modify the workflow to use environment-specific variables

Example workflow change:

```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: ${{ github.event.inputs.environment || 'production' }}
    
    steps:
      # ... existing steps ...
```

## Support

For issues with:
- **Azure setup**: Check Azure Portal for service principal and role assignments
- **GitHub setup**: Check GitHub repository settings for variables and environments
- **Workflow failures**: Review GitHub Actions logs for detailed error messages

## References

- [Azure OIDC with GitHub Actions](https://docs.microsoft.com/azure/developer/github/connect-from-azure)
- [GitHub Environments](https://docs.github.com/actions/deployment/targeting-different-environments/using-environments-for-deployment)
- [Azure Role-Based Access Control](https://docs.microsoft.com/azure/role-based-access-control/overview)
