namespace AiRoleplayChat.Backend.Models;

public class ApiKeyRequest
{
    public string ServiceName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class ApiKeyResponse
{
    public string ServiceName { get; set; } = string.Empty;
    public bool HasKey { get; set; }
}

public class SetKeyVaultUriRequest
{
    public string KeyVaultUri { get; set; } = string.Empty;
}

public class UserApiKeysResponse
{
    public List<string> RegisteredServices { get; set; } = new();
}
