using System.Text.Json;
using Aevatar.Cli.Args;
using Aevatar.Cli.Auth;
using Aevatar.Cli.Config;
using Microsoft.Extensions.Logging;

namespace Aevatar.Cli.Commands;

public class ConfigCommand : BaseHttpCommand
{
    public const string Name = "config";

    private readonly CliConfigService _configService;

    public ConfigCommand(AuthenticationService authService, IHttpClientFactory httpClientFactory, CliConfigService configService)
        : base(authService, httpClientFactory)
    {
        _configService = configService;
    }

    public override async Task ExecuteAsync(CommandLineArgs commandLineArgs)
    {
        var subCommand = GetArgument(commandLineArgs, 0);
        
        if (string.IsNullOrEmpty(subCommand))
        {
            Logger.LogInformation(GetUsageInfo());
            return;
        }

        try
        {
            switch (subCommand.ToLowerInvariant())
            {
                case "show":
                    await ShowConfigAsync(commandLineArgs);
                    break;
                case "init":
                    await InitConfigAsync(commandLineArgs);
                    break;
                case "reset":
                    await ResetConfigAsync(commandLineArgs);
                    break;
                case "set-token":
                    await SetTokenAsync(commandLineArgs);
                    break;
                case "clear-token":
                    await ClearTokenAsync(commandLineArgs);
                    break;
                case "path":
                    ShowConfigPath(commandLineArgs);
                    break;
                default:
                    Logger.LogWarning("Unknown config subcommand: {SubCommand}", subCommand);
                    Logger.LogInformation(GetUsageInfo());
                    break;
            }
        }
        catch (CliUsageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing config command: {Message}", ex.Message);
            throw new CliUsageException($"Config command failed: {ex.Message}", ex);
        }
    }

    private async Task ShowConfigAsync(CommandLineArgs args)
    {
        var config = await _configService.GetConfigAsync();
        
        if (config == null)
        {
            Logger.LogInformation("❌ 配置文件不存在");
            Logger.LogInformation("💡 运行 'aevatar config init' 创建默认配置");
            return;
        }

        if (HasOption(args, "json"))
        {
            OutputJson(config);
            return;
        }

        Logger.LogInformation("");
        Logger.LogInformation("📋 Aevatar CLI 配置");
        Logger.LogInformation("─────────────────────");
        Logger.LogInformation("当前环境: {Environment}", config.CurrentEnvironment);
        
        if (config.Auth != null)
        {
            var hasToken = !string.IsNullOrEmpty(config.Auth.Token);
            var tokenStatus = hasToken ? "✅ 已保存" : "❌ 未保存";
            Logger.LogInformation("认证状态: {Status}", tokenStatus);
            
            if (hasToken && config.Auth.TokenExpiry.HasValue)
            {
                var isExpired = config.Auth.TokenExpiry.Value <= DateTime.UtcNow;
                var expiryStatus = isExpired ? "❌ 已过期" : "✅ 有效";
                Logger.LogInformation("Token状态: {Status} (过期时间: {Expiry})", 
                    expiryStatus, 
                    config.Auth.TokenExpiry.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        Logger.LogInformation("输出格式: {Format}", config.Output?.DefaultFormat ?? "table");
        Logger.LogInformation("超时设置: {Timeout}秒", config.Defaults?.TimeoutSeconds ?? 30);
        Logger.LogInformation("");
        
        _configService.ShowConfigPath();
    }

    private async Task InitConfigAsync(CommandLineArgs args)
    {
        Logger.LogInformation("🔧 初始化CLI配置...");
        
        await _configService.InitializeConfigAsync();
        
        Logger.LogInformation("");
        Logger.LogInformation("✅ 配置初始化完成!");
        Logger.LogInformation("💡 现在可以使用 'aevatar config show' 查看配置");
        Logger.LogInformation("💡 使用 'aevatar util test-auth' 测试认证");
    }

    private async Task ResetConfigAsync(CommandLineArgs args)
    {
        if (!HasOption(args, "confirm"))
        {
            Logger.LogWarning("⚠️  这将删除所有保存的配置，包括认证令牌!");
            Logger.LogWarning("如果确定要重置，请使用: aevatar config reset --confirm");
            return;
        }

        Logger.LogInformation("🔄 重置CLI配置...");
        
        await _configService.ResetConfigAsync();
        
        Logger.LogInformation("✅ 配置已重置");
        Logger.LogInformation("💡 运行 'aevatar config init' 重新初始化配置");
    }

    private async Task SetTokenAsync(CommandLineArgs args)
    {
        var token = GetOption(args, "token");
        if (string.IsNullOrEmpty(token))
        {
            throw new CliUsageException("Token is required. Usage: aevatar config set-token --token <your-token>");
        }

        var expiryHours = int.TryParse(GetOption(args, "expires-hours"), out var hours) ? hours : 24;
        var expiresInSeconds = expiryHours * 3600;

        Logger.LogInformation("💾 保存认证令牌...");
        
        await _configService.SaveTokenAsync(token, expiresInSeconds);
        
        Logger.LogInformation("✅ Token已保存");
        Logger.LogInformation("💡 现在可以使用CLI命令而无需重新认证");
    }

    private async Task ClearTokenAsync(CommandLineArgs args)
    {
        var config = await _configService.GetConfigAsync();
        if (config?.Auth != null)
        {
            config.Auth.Token = null;
            config.Auth.TokenExpiry = null;
            config.Auth.RefreshToken = null;
            
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aevatar", "config.json");
            await File.WriteAllTextAsync(configPath, json);
            
            Logger.LogInformation("✅ 认证令牌已清除");
            Logger.LogInformation("💡 下次使用时将重新进行认证");
        }
        else
        {
            Logger.LogInformation("❌ 没有保存的认证令牌");
        }
    }

    private void ShowConfigPath(CommandLineArgs args)
    {
        _configService.ShowConfigPath();
    }

    public override string GetUsageInfo()
    {
        return @"
Usage: aevatar config <subcommand> [options] [arguments]

Subcommands:
  show                             显示当前配置
  init                             初始化默认配置
  reset                            重置配置 (需要 --confirm)
  set-token                        手动设置认证令牌
  clear-token                      清除保存的认证令牌
  path                             显示配置文件路径

Set Token Options:
  --token <token>                  要保存的认证令牌
  --expires-hours <hours>          令牌有效期小时数 (默认: 24)

Reset Options:
  --confirm                        确认重置操作

Global Options:
  --json                           JSON格式输出 (仅适用于show)

Examples:
  aevatar config show                                    # 显示当前配置
  aevatar config init                                    # 初始化配置
  aevatar config set-token --token eyJhbGciOi...         # 设置令牌
  aevatar config clear-token                             # 清除令牌
  aevatar config reset --confirm                         # 重置配置
  aevatar config path                                    # 显示配置路径

Token管理提示:
  💾 CLI会自动保存认证令牌，避免重复认证
  ⏰ 令牌过期前会自动刷新 (如果支持)
  🔄 如果认证失败，可手动设置有效令牌
";
    }

    public static string GetShortDescription()
    {
        return "Manage CLI configuration and authentication tokens";
    }
}
