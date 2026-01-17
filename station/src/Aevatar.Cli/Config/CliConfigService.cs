using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Cli.Config;

public class CliConfigService : ISingletonDependency
{
    public ILogger<CliConfigService> Logger { get; set; }
    
    private readonly string _configDirectory;
    private readonly string _configFilePath;
    
    public CliConfigService()
    {
        Logger = NullLogger<CliConfigService>.Instance;
        
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configDirectory = Path.Combine(homeDirectory, ".aevatar");
        _configFilePath = Path.Combine(_configDirectory, "config.json");
        
        EnsureConfigDirectoryExists();
    }

    public async Task<TokenInfo?> GetSavedTokenAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                Logger.LogDebug("配置文件不存在: {Path}", _configFilePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(_configFilePath);
            var config = JsonSerializer.Deserialize<CliConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            if (config?.Auth?.Token == null)
            {
                Logger.LogDebug("配置文件中没有保存的token");
                return null;
            }

            var tokenInfo = new TokenInfo
            {
                AccessToken = config.Auth.Token,
                ExpiresAt = config.Auth.TokenExpiry ?? DateTime.MinValue,
                RefreshToken = config.Auth.RefreshToken
            };

            if (tokenInfo.ExpiresAt <= DateTime.UtcNow.AddMinutes(5))
            {
                Logger.LogDebug("保存的token已过期");
                return null;
            }

            Logger.LogDebug("Found saved token with expiry: {Expiry}", 
                tokenInfo.ExpiresAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            
            return tokenInfo;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "读取保存的token失败: {Message}", ex.Message);
            return null;
        }
    }

    public async Task SaveTokenAsync(string accessToken, int expiresInSeconds, string? refreshToken = null)
    {
        try
        {
            var expiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
            
            // Read existing config or create new one
            var config = await GetConfigAsync() ?? new CliConfig();
            
            // Update auth section
            config.Auth ??= new AuthConfig();
            config.Auth.Token = accessToken;
            config.Auth.TokenExpiry = expiresAt;
            config.Auth.RefreshToken = refreshToken;
            config.Auth.LastUpdated = DateTime.UtcNow;

            // Save config
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(_configFilePath, json);
            
            Logger.LogInformation("💾 认证令牌已保存 (有效期至: {Expiry})", expiresAt.ToLocalTime().ToString("MM-dd HH:mm"));
            Logger.LogDebug("配置文件位置: {Path}", _configFilePath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存token失败: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<CliConfig?> GetConfigAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(_configFilePath);
            return JsonSerializer.Deserialize<CliConfig>(json);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "读取配置文件失败: {Message}", ex.Message);
            return null;
        }
    }

    public async Task ResetConfigAsync()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                File.Delete(_configFilePath);
                Logger.LogInformation("✅ 配置文件已重置");
            }
            else
            {
                Logger.LogInformation("配置文件不存在，无需重置");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "重置配置文件失败: {Message}", ex.Message);
            throw;
        }
    }

    public void ShowConfigPath()
    {
        Logger.LogInformation("📁 CLI配置文件位置: {Path}", _configFilePath);
        Logger.LogInformation("📂 配置目录: {Directory}", _configDirectory);
        
        if (File.Exists(_configFilePath))
        {
            var fileInfo = new FileInfo(_configFilePath);
            Logger.LogInformation("📊 文件大小: {Size} bytes", fileInfo.Length);
            Logger.LogInformation("🕒 最后修改: {LastWrite}", fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        else
        {
            Logger.LogInformation("❌ 配置文件不存在");
        }
    }

    public async Task InitializeConfigAsync()
    {
        var config = new CliConfig
        {
            CurrentEnvironment = "local",
            Environments = new Dictionary<string, EnvironmentConfig>
            {
                ["local"] = new EnvironmentConfig
                {
                    Silo = new SiloConfig
                    {
                        GatewayEndpoints = new List<string>
                        {
                            "127.0.0.2:30000",
                            "127.0.0.3:30001", 
                            "127.0.0.4:30002"
                        },
                        ClusterId = "AevatarSiloCluster",
                        ServiceId = "AevatarBasicService",
                        PreferredSilo = "127.0.0.2:30000"
                    },
                    Auth = new AuthConfig
                    {
                        AuthServer = "http://localhost:7001",
                        HttpApiHost = "http://localhost:7002",
                        AutoAuth = true
                    },
                    Services = new ServicesConfig
                    {
                        AuthServer = "http://localhost:7001",
                        HttpApiHost = "http://localhost:7002",
                        DeveloperHost = "http://localhost:7003",
                        AspireEndpoint = "https://localhost:18888",
                        OrleansDashboards = new List<string>
                        {
                            "http://127.0.0.2:8080",
                            "http://127.0.0.3:8081",
                            "http://127.0.0.4:8082"
                        }
                    }
                }
            },
            Output = new OutputConfig
            {
                DefaultFormat = "table",
                ColorEnabled = true,
                VerboseLogging = false
            },
            Defaults = new DefaultsConfig
            {
                TimeoutSeconds = 30
            }
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(_configFilePath, json);
        
        Logger.LogInformation("✅ 配置文件已初始化: {Path}", _configFilePath);
    }

    private void EnsureConfigDirectoryExists()
    {
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
            Logger.LogDebug("创建配置目录: {Directory}", _configDirectory);
        }
    }

    public class TokenInfo
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string? RefreshToken { get; set; }
    }

    public class CliConfig
    {
        public string CurrentEnvironment { get; set; } = "local";
        public Dictionary<string, EnvironmentConfig> Environments { get; set; } = new();
        public OutputConfig Output { get; set; } = new();
        public DefaultsConfig Defaults { get; set; } = new();
        public AuthConfig? Auth { get; set; }
    }

    public class EnvironmentConfig
    {
        public SiloConfig Silo { get; set; } = new();
        public AuthConfig Auth { get; set; } = new();
        public ServicesConfig Services { get; set; } = new();
    }

    public class SiloConfig
    {
        public List<string> GatewayEndpoints { get; set; } = new();
        public string ClusterId { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
        public string? PreferredSilo { get; set; }
    }

    public class AuthConfig
    {
        public string AuthServer { get; set; } = string.Empty;
        public string HttpApiHost { get; set; } = string.Empty;
        public bool AutoAuth { get; set; } = true;
        public string? Token { get; set; }
        public DateTime? TokenExpiry { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class ServicesConfig
    {
        public string AuthServer { get; set; } = string.Empty;
        public string HttpApiHost { get; set; } = string.Empty;
        public string DeveloperHost { get; set; } = string.Empty;
        public string AspireEndpoint { get; set; } = string.Empty;
        public List<string> OrleansDashboards { get; set; } = new();
    }

    public class OutputConfig
    {
        public string DefaultFormat { get; set; } = "table";
        public bool ColorEnabled { get; set; } = true;
        public bool VerboseLogging { get; set; } = false;
    }

    public class DefaultsConfig
    {
        public string? OrganizationId { get; set; }
        public string? ProjectId { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
    }
}
