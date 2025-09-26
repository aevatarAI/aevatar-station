using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aevatar.Cli.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Cli.Auth;

public class AuthenticationService : ISingletonDependency
{
    public ILogger<AuthenticationService> Logger { get; set; }
    
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CliConfigService _configService;
    private string? _accessToken;
    private DateTime _tokenExpiry;
    
    // Local development default credentials
    private const string DefaultAuthServer = "http://localhost:7001";
    private const string DefaultApiHost = "http://localhost:7002";
    private const string AdminUsername = "admin";
    private const string AdminPassword = "1q2W3e*";
    private const string ClientId = "Aevatar001";
    private const string ClientSecret = "123456";

    public AuthenticationService(IHttpClientFactory httpClientFactory, CliConfigService configService)
    {
        _httpClientFactory = httpClientFactory;
        _configService = configService;
        Logger = NullLogger<AuthenticationService>.Instance;
    }

    public async Task<string> GetAccessTokenAsync(string authServerUrl = DefaultAuthServer, string apiHostUrl = DefaultApiHost)
    {
        // First check if we have a valid in-memory token
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
        {
            return _accessToken;
        }

        // Check for saved token in config file
        var savedToken = await _configService.GetSavedTokenAsync();
        if (savedToken != null && !string.IsNullOrEmpty(savedToken.AccessToken))
        {
            _accessToken = savedToken.AccessToken;
            _tokenExpiry = savedToken.ExpiresAt;
            // Simply inform using saved token without verbose logging
            Logger.LogInformation("🔑 使用本地保存的认证令牌...");
            return _accessToken;
        }

        Logger.LogInformation("🔍 检测到本地环境，正在自动认证...");
        
        try
        {
            // Step 1: Get admin token
            var adminToken = await GetAdminTokenAsync(authServerUrl);
            if (string.IsNullOrEmpty(adminToken))
            {
                throw new Exception("Failed to get admin token");
            }

            // Step 2: Register CLI client
            await RegisterCliClientAsync(apiHostUrl, adminToken);

            // Step 3: Get client token and parse expiry
            var (clientToken, expiresIn) = await GetClientTokenWithExpiryAsync(authServerUrl);
            if (string.IsNullOrEmpty(clientToken))
            {
                throw new Exception("Failed to get client token");
            }

            _accessToken = clientToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);
            
            // Step 4: Save token to config file
            await _configService.SaveTokenAsync(clientToken, expiresIn);
            
            Logger.LogInformation("✅ 自动认证成功!");
            
            return _accessToken;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ 自动认证失败: {Message}", ex.Message);
            
            // Show token saving instructions when auth fails
            ShowTokenSavingInstructions();
            
            throw new CliUsageException("Authentication failed. Please ensure Aevatar.Aspire services are running.", ex);
        }
    }

    private async Task<string> GetAdminTokenAsync(string authServerUrl)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        var requestUrl = $"{authServerUrl}/connect/token";
        var formData = new Dictionary<string, string>
        {
            {"grant_type", "password"},
            {"client_id", "AevatarAuthServer"},
            {"username", AdminUsername},
            {"password", AdminPassword},
            {"scope", "Aevatar"}
        };

        var content = new FormUrlEncodedContent(formData);
        
        Logger.LogDebug("Requesting admin token from {Url}", requestUrl);
        var response = await client.PostAsync(requestUrl, content);
        
        var responseBody = await response.Content.ReadAsStringAsync();
        Logger.LogDebug("Auth server response: {Response}", responseBody);
        
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogError("Auth server returned {StatusCode}: {Response}", response.StatusCode, responseBody);
            throw new Exception($"Auth server returned {response.StatusCode}: {responseBody}");
        }
        
        try
        {
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody);
            if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
            {
                throw new Exception("No access token in response");
            }
            return tokenResponse.AccessToken;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to parse token response: {Response}", responseBody);
            throw new Exception($"Failed to parse token response: {ex.Message}");
        }
    }

    private async Task RegisterCliClientAsync(string apiHostUrl, string adminToken)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        
        var requestUrl = $"{apiHostUrl}/api/users/registerClient?clientId={ClientId}&clientSecret={ClientSecret}&corsUrls=s";
        
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Accept", "*/*");
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
        
        Logger.LogDebug("Registering CLI client at {Url}", requestUrl);
        var response = await client.PostAsync(requestUrl, new StringContent(string.Empty));
        response.EnsureSuccessStatusCode();
        
        Logger.LogDebug("CLI client registered successfully");
    }

    private async Task<string> GetClientTokenAsync(string authServerUrl)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        var requestUrl = $"{authServerUrl}/connect/token";
        var formData = new Dictionary<string, string>
        {
            {"grant_type", "client_credentials"},
            {"client_id", ClientId},
            {"client_secret", ClientSecret},
            {"scope", "Aevatar"}
        };

        var content = new FormUrlEncodedContent(formData);
        
        Logger.LogDebug("Requesting client token from {Url}", requestUrl);
        var response = await client.PostAsync(requestUrl, content);
        
        var responseBody = await response.Content.ReadAsStringAsync();
        Logger.LogDebug("Client token response: {Response}", responseBody);
        
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogError("Auth server returned {StatusCode}: {Response}", response.StatusCode, responseBody);
            throw new Exception($"Auth server returned {response.StatusCode}: {responseBody}");
        }
        
        try
        {
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody);
            if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
            {
                throw new Exception("No access token in response");
            }
            return tokenResponse.AccessToken;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to parse token response: {Response}", responseBody);
            throw new Exception($"Failed to parse token response: {ex.Message}");
        }
    }

    private async Task<(string Token, int ExpiresIn)> GetClientTokenWithExpiryAsync(string authServerUrl)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        var requestUrl = $"{authServerUrl}/connect/token";
        var formData = new Dictionary<string, string>
        {
            {"grant_type", "client_credentials"},
            {"client_id", ClientId},
            {"client_secret", ClientSecret},
            {"scope", "Aevatar"}
        };

        var content = new FormUrlEncodedContent(formData);
        
        Logger.LogDebug("Requesting client token from {Url}", requestUrl);
        var response = await client.PostAsync(requestUrl, content);
        
        var responseBody = await response.Content.ReadAsStringAsync();
        Logger.LogDebug("Client token response: {Response}", responseBody);
        
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogError("Auth server returned {StatusCode}: {Response}", response.StatusCode, responseBody);
            throw new Exception($"Auth server returned {response.StatusCode}: {responseBody}");
        }
        
        try
        {
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody);
            if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
            {
                throw new Exception("No access token in response");
            }
            return (tokenResponse.AccessToken, tokenResponse.ExpiresIn);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to parse token response: {Response}", responseBody);
            throw new Exception($"Failed to parse token response: {ex.Message}");
        }
    }

    private void ShowTokenSavingInstructions()
    {
        Logger.LogInformation("");
        Logger.LogInformation("💡 如何手动保存Access Token:");
        Logger.LogInformation("─────────────────────────────");
        Logger.LogInformation("1. 从浏览器或其他工具获取有效的access_token");
        Logger.LogInformation("2. 运行命令保存: aevatar config set-token <your-token>");
        Logger.LogInformation("3. 或者直接编辑配置文件: ~/.aevatar/config.json");
        Logger.LogInformation("");
        Logger.LogInformation("📋 配置文件格式示例:");
        Logger.LogInformation("{");
        Logger.LogInformation("  \"auth\": {");
        Logger.LogInformation("    \"token\": \"your-access-token-here\",");
        Logger.LogInformation("    \"tokenExpiry\": \"2024-12-31T23:59:59Z\"");
        Logger.LogInformation("  }");
        Logger.LogInformation("}");
        Logger.LogInformation("");
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry;

    public async Task<bool> TestConnectionAsync(string authServerUrl = DefaultAuthServer, string apiHostUrl = DefaultApiHost)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            // Test auth server
            var authResponse = await client.GetAsync($"{authServerUrl}/.well-known/openid_configuration");
            if (!authResponse.IsSuccessStatusCode)
            {
                return false;
            }
            
            // Test API host
            var apiResponse = await client.GetAsync($"{apiHostUrl}/health");
            return apiResponse.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        
        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }
}
