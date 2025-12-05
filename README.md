![Header image](https://github.com/DougChisholm/App-Mod-Booster/blob/main/repo-header-booster.png)

# Expense Management System - Modern Azure Application

A modern, cloud-native expense management application built with ASP.NET Core 8, deployed on Azure with best-practice security and monitoring. This application was generated from legacy screenshots and a database schema using AI-powered modernization.

## ✨ Features

- 📊 **Expense Tracking**: Create, view, and manage expenses
- ✅ **Approval Workflow**: Approve or reject expense submissions
- 📈 **Reporting**: View expense summaries by category and status
- 🤖 **AI Chat Assistant**: Natural language interface for expense management (optional)
- 🔒 **Secure by Default**: Managed Identity, Entra ID authentication, no stored secrets
- 📱 **Modern UI**: Clean, responsive interface with Bootstrap 5
- 🚀 **RESTful API**: Full Swagger/OpenAPI documentation
- 📉 **Application Insights**: Built-in monitoring and telemetry

## 🏗️ Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture diagrams and component descriptions.

### Technology Stack

- **Frontend**: Razor Pages, Bootstrap 5, Vanilla JavaScript
- **Backend**: ASP.NET Core 8 Web API
- **Database**: Azure SQL Database with stored procedures
- **Authentication**: Azure Managed Identity + Entra ID
- **Monitoring**: Application Insights + Log Analytics
- **AI**: Azure OpenAI (GPT-4o) + Azure AI Search (optional)
- **Infrastructure**: Bicep (Infrastructure as Code)
- **CI/CD**: GitHub Actions with OIDC

## 🚀 Quick Start

### Prerequisites

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [PowerShell 7+](https://github.com/PowerShell/PowerShell/releases) (recommended)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [go-sqlcmd](https://github.com/microsoft/go-sqlcmd) - Install with `winget install sqlcmd` on Windows
- Azure subscription with appropriate permissions

### Deployment Steps

#### 1. Login to Azure

```powershell
az login
```

#### 2. Deploy Infrastructure

```powershell
# Basic deployment (without GenAI)
.\deploy-infra\deploy.ps1 -ResourceGroup "rg-expensemgmt-20250105" -Location "uksouth"

# With AI Chat features
.\deploy-infra\deploy.ps1 -ResourceGroup "rg-expensemgmt-20250105" -Location "uksouth" -DeployGenAI
```

This creates:
- Azure App Service (Standard S1)
- Azure SQL Database (Basic tier)
- User-Assigned Managed Identity
- Log Analytics Workspace
- Application Insights
- Azure OpenAI + AI Search (if -DeployGenAI specified)

#### 3. Deploy Application

```powershell
.\deploy-app\deploy.ps1
```

The script automatically reads configuration from the infrastructure deployment, so no parameters are needed.

#### 4. Access Your Application

After deployment completes, visit:

- **Main App**: `https://{your-app-name}.azurewebsites.net/Index`
- **API Docs**: `https://{your-app-name}.azurewebsites.net/swagger`
- **AI Chat**: `https://{your-app-name}.azurewebsites.net/Chat` (if GenAI deployed)

## 📖 Documentation

- **[Infrastructure Deployment Guide](deploy-infra/README.md)** - Detailed Bicep and PowerShell documentation
- **[Application Deployment Guide](deploy-app/README.md)** - Application build and deployment
- **[CI/CD Setup Guide](.github/CICD-SETUP.md)** - GitHub Actions with OIDC
- **[Architecture Diagram](ARCHITECTURE.md)** - System architecture and data flows

## 🛡️ Security Features

- **No Passwords**: All authentication uses Azure Managed Identity
- **Entra ID Only**: SQL Server configured for Azure AD authentication only
- **HTTPS Enforced**: All traffic encrypted (TLS 1.2+)
- **Least Privilege**: Managed Identity has minimal required permissions
- **Audit Logging**: All database operations logged
- **Security by Default**: Following MCAPS security policies

## 🤖 AI Chat Assistant (Optional)

When deployed with `-DeployGenAI`, the application includes an AI-powered chat interface that can:

- Answer questions about expenses
- Create new expense records
- Retrieve expense summaries
- Filter expenses by various criteria
- Provide natural language interface to all APIs

**Example Queries**:
- "Show me all pending expenses"
- "Create a new expense for £50 for lunch today"
- "What are my expenses this month?"
- "How much has been approved for user 1?"

## 🧪 API Documentation

Full API documentation available at `/swagger`

## 🔄 CI/CD Pipeline

GitHub Actions workflow for automated deployment. See [.github/CICD-SETUP.md](.github/CICD-SETUP.md) for setup instructions.

## 🐛 Troubleshooting

See the deployment guides for detailed troubleshooting:
- [Infrastructure Troubleshooting](deploy-infra/README.md#troubleshooting)
- [Application Troubleshooting](deploy-app/README.md#troubleshooting)

## 🔧 Local Development

Create `src/ExpenseManagement/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=Northwind;Authentication=Active Directory Default;"
  }
}
```

Run:
```bash
cd src/ExpenseManagement
dotnet run
```

Access at `https://localhost:5001/Index`

---

## 📚 App Modernization Guide

This application demonstrates how GitHub Copilot can modernize legacy applications:

### Steps to Modernize Your App:

1. **Fork this repo** 
2. **Replace the assets**:
   - Add your legacy app screenshots to `Legacy-Screenshots/`
   - Add your database schema to `Database-Schema/`
3. **Use GitHub Copilot** with the prompt: "modernise my app"
4. **Review the PR** created by Copilot (can take up to 30 minutes)
5. **Deploy** using the PowerShell scripts provided

### For Microsoft Employees

Supporting slides: [Here](<https://microsofteur-my.sharepoint.com/:p:/g/personal/dchisholm_microsoft_com/IQAY41LQ12fjSIfFz3ha4hfFAZc7JQQuWaOrF7ObgxRK6f4?e=p6arJs>)

---

**Built with ❤️ using Azure, .NET, and GitHub Copilot**
