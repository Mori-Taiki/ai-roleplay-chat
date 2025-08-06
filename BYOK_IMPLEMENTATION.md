# BYOK (Bring Your Own Key) Implementation

This document describes the BYOK implementation for Azure Key Vault integration in the AI Roleplay Chat application.

## Overview

The BYOK feature allows users to store and manage their own API keys securely in Azure Key Vault, providing enhanced security and control over external service integrations (Gemini AI, Replicate, etc.).

## Architecture

### Components

1. **ApiKeyService**: Core service for managing API keys in Azure Key Vault
2. **ApiKeyController**: REST API endpoints for key management
3. **Enhanced User Entity**: Extended with KeyVaultUri field
4. **Service Integration**: Modified GeminiService and ReplicateService to use user-specific keys

### Database Schema Changes

```sql
-- Added to Users table
ALTER TABLE Users ADD COLUMN KeyVaultUri TEXT NULL;
```

## API Endpoints

### 1. Register API Key
- **POST** `/api/ApiKey`
- **Body**: `{ "serviceName": "Gemini", "apiKey": "your-api-key" }`
- **Description**: Stores user's API key securely in Key Vault

### 2. Check API Key Existence
- **GET** `/api/ApiKey/{serviceName}`
- **Response**: `{ "serviceName": "Gemini", "hasKey": true }`
- **Description**: Checks if user has registered an API key for the service

### 3. Delete API Key
- **DELETE** `/api/ApiKey/{serviceName}`
- **Description**: Removes user's API key from Key Vault

### 4. Set Key Vault URI
- **POST** `/api/ApiKey/keyvault-uri`
- **Body**: `{ "keyVaultUri": "https://your-keyvault.vault.azure.net/" }`
- **Description**: Sets user's custom Key Vault URI

### 5. Get User API Keys Summary
- **GET** `/api/ApiKey`
- **Response**: `{ "registeredServices": ["Gemini", "Replicate"], "keyVaultUri": "..." }`
- **Description**: Lists all services with registered keys

## Azure Key Vault Configuration

### Authentication
The implementation uses `DefaultAzureCredential` which supports:
- Managed Identity (recommended for production)
- Service Principal
- User credentials for development

### Secret Naming Convention
Secrets are stored with the following naming pattern:
```
user-{userId}-{serviceName}-apikey
```

Examples:
- `user-123-gemini-apikey`
- `user-123-replicate-apikey`

## Service Integration

### Fallback Strategy
All AI services (GeminiService, ReplicateService) implement a fallback strategy:
1. Try to retrieve user-specific API key from Key Vault
2. If not found or error occurs, use system default API key
3. Log appropriate messages for debugging

### Usage Example
```csharp
// Services automatically use user keys when available
var response = await geminiService.GenerateChatResponseAsync(
    prompt, 
    systemPrompt, 
    history, 
    userId: currentUserId,  // Pass user ID for BYOK
    cancellationToken
);
```

## Security Considerations

1. **Key Vault Access**: Users need appropriate access policies on their Key Vault
2. **URI Validation**: Key Vault URIs are validated to ensure proper format
3. **Error Handling**: Sensitive information is not exposed in error messages
4. **Audit Trail**: All operations are logged for security monitoring

## Configuration

### appsettings.json
```json
{
  "KeyVault": {
    "VaultUri": "https://default-keyvault.vault.azure.net/"
  }
}
```

### Environment Variables
- `GOOGLE_CLOUD_PROJECT`: For Vertex AI integration
- `REPLICATE_API_TOKEN`: Default Replicate API token
- `Gemini:ApiKey`: Default Gemini API key

## Error Handling

The implementation provides graceful error handling:
- Invalid Key Vault URIs return 400 Bad Request
- Missing authentication returns 401 Unauthorized
- Key Vault access errors fall back to default keys
- All errors are logged with appropriate detail levels

## Testing

The implementation includes:
- Unit tests for service logic
- Integration tests for API endpoints
- Error handling validation
- Fallback mechanism verification

## Deployment Considerations

1. **Managed Identity**: Configure Managed Identity for the App Service
2. **Key Vault Access Policies**: Grant appropriate permissions to the Managed Identity
3. **Environment Variables**: Set default API keys for fallback scenarios
4. **Monitoring**: Set up alerts for Key Vault access failures

## Future Enhancements

1. **Bulk Key Management**: Import/export multiple keys
2. **Key Rotation**: Automatic key rotation capabilities
3. **Usage Analytics**: Track API key usage and costs
4. **Additional Services**: Support for more AI service providers