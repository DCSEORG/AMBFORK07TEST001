# Architecture Diagram

## Expense Management System - Azure Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Azure Cloud                                         │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                     Resource Group                                   │   │
│  │                                                                       │   │
│  │  ┌──────────────────────────────────────────────────────────────┐  │   │
│  │  │              Azure App Service (Linux, .NET 8)               │  │   │
│  │  │                                                               │  │   │
│  │  │  ┌────────────────────┐    ┌─────────────────────────┐     │  │   │
│  │  │  │  Razor Pages UI    │    │  Web API Controllers   │     │  │   │
│  │  │  │  - Index           │    │  - Expenses API        │     │  │   │
│  │  │  │  - Chat            │    │  - Users API           │     │  │   │
│  │  │  │  - Error Handling  │    │  - Categories API      │     │  │   │
│  │  │  └────────────────────┘    └─────────────────────────┘     │  │   │
│  │  │                                                               │  │   │
│  │  └───────────────────┬──────────────────────────────────────────┘  │   │
│  │                      │                                              │   │
│  │                      │ Uses Managed Identity                       │   │
│  │                      │ for Authentication                           │   │
│  │                      │                                              │   │
│  │         ┌────────────┼────────────┬──────────────────┬──────────┐ │   │
│  │         │            │            │                  │          │ │   │
│  │         ▼            ▼            ▼                  ▼          ▼ │   │
│  │  ┌───────────┐ ┌──────────┐ ┌─────────┐     ┌──────────┐ ┌──────┐│   │
│  │  │  User-    │ │  Azure   │ │  Log    │     │  Azure   │ │ Azure││   │
│  │  │  Assigned │ │  SQL     │ │Analytics│     │  OpenAI  │ │Search││   │
│  │  │  Managed  │ │ Database │ │Workspace│     │  (GPT-4o)│ │      ││   │
│  │  │  Identity │ │          │ │         │     │          │ │      ││   │
│  │  └───────────┘ └──────────┘ └─────────┘     └──────────┘ └──────┘│   │
│  │       │              │            │               │          │    │   │
│  │       │              │            │               │          │    │   │
│  │       │              │            ▼               │          │    │   │
│  │       │              │      ┌──────────────┐      │          │    │   │
│  │       │              │      │ Application  │      │          │    │   │
│  │       │              │      │  Insights    │      │          │    │   │
│  │       │              │      └──────────────┘      │          │    │   │
│  │       │              │                            │          │    │   │
│  │       └──────────────┴───── SQL Authentication ───┘          │    │   │
│  │                              (Entra ID Only)                  │    │   │
│  │                                                                    │   │
│  │  ┌─────────────────────────────────────────────────────────────┐ │   │
│  │  │                Security & Authentication                     │ │   │
│  │  │  • Entra ID (Azure AD) authentication for SQL               │ │   │
│  │  │  • No passwords or connection strings stored                │ │   │
│  │  │  • User-assigned Managed Identity for all services          │ │   │
│  │  │  • HTTPS only (TLS 1.2+)                                    │ │   │
│  │  │  • Azure RBAC for resource access                           │ │   │
│  │  └─────────────────────────────────────────────────────────────┘ │   │
│  └────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘

External Users
     │
     │ HTTPS
     │
     ▼
┌─────────────┐
│   Browser   │
│  (React/    │
│   Vanilla   │
│     JS)     │
└─────────────┘
```

## Component Descriptions

### Core Application

**Azure App Service**
- **Type**: Web App (Linux)
- **Runtime**: .NET 8
- **Tier**: Standard S1 (no cold starts)
- **Purpose**: Hosts the ASP.NET Core application with Razor Pages UI and Web API

### Data Layer

**Azure SQL Database**
- **Tier**: Basic (suitable for dev/test)
- **Database**: Northwind
- **Authentication**: Entra ID only (no SQL auth)
- **Access**: Managed via stored procedures
- **Connection**: Uses Managed Identity

### Identity & Security

**User-Assigned Managed Identity**
- **Purpose**: Secure, credential-less authentication
- **Access to**: Azure SQL, Azure OpenAI, Azure AI Search
- **No Secrets**: No passwords or connection strings stored
- **Permissions**: 
  - SQL: db_datareader, db_datawriter, EXECUTE
  - OpenAI: Cognitive Services OpenAI User
  - Search: Search Index Data Reader

### Monitoring & Observability

**Log Analytics Workspace**
- Centralized logging for all resources
- Query logs with KQL (Kusto Query Language)

**Application Insights**
- Application performance monitoring
- Exception tracking
- Dependency tracking
- Custom metrics and telemetry

### Optional: GenAI Features

**Azure OpenAI** (Sweden Central)
- **Model**: GPT-4o
- **Capacity**: 8
- **Purpose**: Powers the AI chat assistant
- **Authentication**: Managed Identity

**Azure AI Search**
- **Tier**: Basic
- **Purpose**: RAG (Retrieval-Augmented Generation) support
- **Authentication**: Managed Identity

## Data Flow

### User Interaction Flow

1. **User** accesses application via browser (HTTPS)
2. **App Service** serves Razor Pages UI
3. **User** interacts with pages (view expenses, submit, etc.)
4. **JavaScript** calls Web API endpoints
5. **API Controllers** call Service layer
6. **Services** execute stored procedures via Managed Identity
7. **Azure SQL** returns data
8. **Response** flows back to user

### AI Chat Flow (if GenAI enabled)

1. **User** sends message in Chat UI
2. **Chat API** receives request
3. **ChatService** authenticates to Azure OpenAI using Managed Identity
4. **Azure OpenAI** processes message with function calling
5. **Functions** call application APIs to retrieve/modify data
6. **AI** generates natural language response
7. **Response** displayed to user

### Monitoring Flow

1. **All components** send telemetry to Application Insights
2. **Logs** aggregated in Log Analytics Workspace
3. **Metrics** available for alerting and dashboards
4. **Diagnostics** available in Azure Portal

## Security Features

- **No Secrets**: All authentication uses Managed Identity
- **Entra ID Only**: SQL Server doesn't allow username/password
- **HTTPS Enforced**: All traffic encrypted (TLS 1.2+)
- **Firewall Rules**: SQL Server only accessible from Azure services
- **RBAC**: Role-based access control for all resources
- **Audit Logs**: All database operations logged

## Deployment Architecture

### Infrastructure as Code (Bicep)
- `main.bicep`: Orchestrates all modules
- `modules/`: Individual resource definitions
- `deploy.ps1`: PowerShell automation script

### CI/CD Pipeline (GitHub Actions)
- OIDC authentication (no secrets)
- Automated infrastructure deployment
- Application build and deployment
- Service Principal with federated credentials

## Scalability

- **App Service**: Can scale up (vertical) or out (horizontal)
- **SQL Database**: Can upgrade to higher tiers for more resources
- **Application Insights**: No limits on ingestion
- **Azure OpenAI**: Can increase capacity or add deployments

## Cost Considerations

- **App Service S1**: ~$70/month (no cold starts)
- **SQL Basic**: ~$5/month
- **Log Analytics**: Pay-per-GB ingested
- **Application Insights**: First 5GB/month free
- **Azure OpenAI** (optional): Pay-per-token usage
- **AI Search** (optional): Basic tier ~$75/month

## High Availability

- **App Service**: 99.95% SLA
- **SQL Database**: 99.99% SLA
- **Application Insights**: 99.9% SLA
- **Azure OpenAI**: 99.9% SLA

## Best Practices Implemented

✅ Managed Identity for authentication
✅ Infrastructure as Code (Bicep)
✅ Separation of infrastructure and application deployment
✅ Centralized logging and monitoring
✅ API-first design with Swagger documentation
✅ Graceful error handling with fallback data
✅ Secure by default (Entra ID only, HTTPS only)
✅ GitOps-friendly deployment
✅ CI/CD with OIDC (no secrets in GitHub)

## References

- [Azure App Service Best Practices](https://learn.microsoft.com/en-us/azure/app-service/app-service-best-practices)
- [Azure SQL Security Best Practices](https://learn.microsoft.com/en-us/azure/azure-sql/database/security-best-practice)
- [Managed Identity Best Practices](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/managed-identity-best-practice-recommendations)
