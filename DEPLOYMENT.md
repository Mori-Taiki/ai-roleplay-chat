# Deployment Guide

This document describes the automated deployment setup for the AI Roleplay Chat application.

## Overview

The application is deployed to Azure using GitHub Actions with the following architecture:
- **Frontend**: Azure Static Web Apps
- **Backend**: Azure App Service (Linux)
- **Database**: Azure Database for MySQL

## GitHub Actions Workflow

The deployment is triggered automatically when code is pushed to the `main` branch. The workflow includes three jobs:

1. **Frontend Deployment** (`build_and_deploy_job`): Deploys React frontend to Static Web Apps
2. **Backend Deployment** (`deploy_backend_job`): Deploys ASP.NET Core backend to App Service
3. **PR Cleanup** (`close_pull_request_job`): Cleans up preview deployments when PRs are closed

## Required GitHub Secrets

### Existing Secrets (Frontend)
- `AZURE_STATIC_WEB_APPS_API_TOKEN_BLUE_PLANT_09D009000`: Static Web Apps deployment token
- `PRODUCTION_API_URL`: Backend API base URL
- `PRODUCTION_B2C_CLIENT_ID`: Azure AD B2C client ID
- `PRODUCTION_B2C_AUTHORITY`: Azure AD B2C authority URL
- `PRODUCTION_B2C_KNOWN_AUTHORITIES`: Azure AD B2C known authorities
- `PRODUCTION_B2C_REDIRECT_URI`: Azure AD B2C redirect URI
- `PRODUCTION_B2C_API_SCOPE_URI`: Azure AD B2C API scope URI

### New Secrets (Backend)
Add the following secrets to your GitHub repository:

- `AZURE_APP_SERVICE_NAME`: The name of your Azure App Service
- `AZURE_APP_SERVICE_PUBLISH_PROFILE`: The publish profile XML content from Azure App Service

## Setting up App Service Deployment

### 1. Get the Publish Profile

1. Go to your Azure App Service in the Azure Portal
2. Click **Get publish profile** in the Overview blade
3. Copy the entire contents of the downloaded `.publishsettings` file
4. Add it as the `AZURE_APP_SERVICE_PUBLISH_PROFILE` secret in GitHub

### 2. Set the App Service Name

1. Add the App Service name as the `AZURE_APP_SERVICE_NAME` secret in GitHub
2. This should match the name shown in the Azure Portal

### 3. Configure App Service Settings

Ensure your App Service has the following application settings configured:

#### Required Settings
- `AzureAdB2C:ClientId`: Azure AD B2C client ID
- `AzureAdB2C:TenantId`: Azure AD B2C tenant ID
- `AzureAdB2C:ApiScopeUrl`: Azure AD B2C API scope URL
- `ConnectionStrings:DefaultConnection`: MySQL connection string
- `GOOGLE_CLOUD_PROJECT`: Google Cloud project ID for Vertex AI
- `KeyVault:VaultUri`: Azure Key Vault URI
- `FrontendUrl`: Frontend URL for CORS configuration

#### Optional Settings
- `Gemini:ApiKey`: Default Gemini API key (fallback)
- `REPLICATE_API_TOKEN`: Default Replicate API token (fallback)

## Deployment Process

1. **Trigger**: Push to `main` branch or create/update PR
2. **Frontend**: Always deployed to Static Web Apps (including PR previews)
3. **Backend**: Only deployed to App Service on `main` branch pushes
4. **Cleanup**: Preview deployments cleaned up when PRs are closed

## Monitoring Deployment

1. Check GitHub Actions tab for workflow status
2. Review deployment logs for any errors
3. Verify application functionality after deployment

## Troubleshooting

### Common Issues

1. **Build Failures**: Check .NET SDK version compatibility
2. **Authentication Issues**: Verify Azure AD B2C settings
3. **Database Connection**: Ensure connection string is correct
4. **API Calls Failing**: Check CORS settings and frontend URL configuration

### Debug Steps

1. Check GitHub Actions logs for specific error messages
2. Verify all required secrets are set correctly
3. Ensure App Service configuration matches local development
4. Test database connectivity from the App Service

## Manual Deployment (Emergency)

If automated deployment fails, you can deploy manually:

### Backend
```bash
cd backend
dotnet publish -c Release
# Upload publish folder to App Service via Azure Portal or Azure CLI
```

### Frontend
```bash
cd frontend
npm run build
# Deploy dist folder to Static Web Apps via Azure Portal or Azure CLI
```