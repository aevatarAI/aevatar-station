using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Abstractions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Application.Configuration;

/// <summary>
/// In-memory configuration provider for development and testing
/// 用于开发和测试的内存配置提供商
/// </summary>
public class MemoryConfigurationProvider : IConfigurationProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, string> _configurations;
    private readonly ILogger<MemoryConfigurationProvider> _logger;

    public MemoryConfigurationProvider(ILogger<MemoryConfigurationProvider> logger)
    {
        _configurations = new ConcurrentDictionary<string, string>();
        _logger = logger;
    }

    public Task<T?> GetConfigurationAsync<T>(string key, ConfigurationScope scope, string scopeId) where T : class
    {
        var compositeKey = BuildCompositeKey(scope, scopeId, key);
        
        if (_configurations.TryGetValue(compositeKey, out var jsonValue))
        {
            try
            {
                var value = JsonSerializer.Deserialize<T>(jsonValue);
                _logger.LogDebug("Retrieved configuration: {Key} from scope {Scope}:{ScopeId}", key, scope, scopeId);
                return Task.FromResult(value);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize configuration: {Key} from scope {Scope}:{ScopeId}", key, scope, scopeId);
                return Task.FromResult<T?>(null);
            }
        }

        _logger.LogDebug("Configuration not found: {Key} in scope {Scope}:{ScopeId}", key, scope, scopeId);
        return Task.FromResult<T?>(null);
    }

    public Task SetConfigurationAsync<T>(string key, T configuration, ConfigurationScope scope, string scopeId) where T : class
    {
        var compositeKey = BuildCompositeKey(scope, scopeId, key);
        var jsonValue = JsonSerializer.Serialize(configuration);
        
        _configurations.AddOrUpdate(compositeKey, jsonValue, (_, _) => jsonValue);
        
        _logger.LogDebug("Set configuration: {Key} in scope {Scope}:{ScopeId}", key, scope, scopeId);
        return Task.CompletedTask;
    }

    public Task DeleteConfigurationAsync(string key, ConfigurationScope scope, string scopeId)
    {
        var compositeKey = BuildCompositeKey(scope, scopeId, key);
        
        if (_configurations.TryRemove(compositeKey, out _))
        {
            _logger.LogDebug("Deleted configuration: {Key} from scope {Scope}:{ScopeId}", key, scope, scopeId);
        }
        else
        {
            _logger.LogDebug("Configuration not found for deletion: {Key} in scope {Scope}:{ScopeId}", key, scope, scopeId);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, ConfigurationScope scope, string scopeId)
    {
        var compositeKey = BuildCompositeKey(scope, scopeId, key);
        var exists = _configurations.ContainsKey(compositeKey);
        
        _logger.LogDebug("Configuration exists check: {Key} in scope {Scope}:{ScopeId} = {Exists}", key, scope, scopeId, exists);
        return Task.FromResult(exists);
    }

    public async Task<T?> GetConfigurationWithCascadeAsync<T>(string key, string? workflowId = null, string? projectId = null, string? userId = null) where T : class
    {
        // Cascade resolution: Workflow → Project → User → System
        // 级联解析：工作流 → 项目 → 用户 → 系统

        // Try Workflow scope first
        if (!string.IsNullOrEmpty(workflowId))
        {
            var workflowConfig = await GetConfigurationAsync<T>(key, ConfigurationScope.Workflow, workflowId);
            if (workflowConfig != null)
            {
                _logger.LogDebug("Configuration resolved from Workflow scope: {Key}", key);
                return workflowConfig;
            }
        }

        // Try Project scope
        if (!string.IsNullOrEmpty(projectId))
        {
            var projectConfig = await GetConfigurationAsync<T>(key, ConfigurationScope.Project, projectId);
            if (projectConfig != null)
            {
                _logger.LogDebug("Configuration resolved from Project scope: {Key}", key);
                return projectConfig;
            }
        }

        // Try User scope
        if (!string.IsNullOrEmpty(userId))
        {
            var userConfig = await GetConfigurationAsync<T>(key, ConfigurationScope.User, userId);
            if (userConfig != null)
            {
                _logger.LogDebug("Configuration resolved from User scope: {Key}", key);
                return userConfig;
            }
        }

        // Try System scope
        var systemConfig = await GetConfigurationAsync<T>(key, ConfigurationScope.System, "global");
        if (systemConfig != null)
        {
            _logger.LogDebug("Configuration resolved from System scope: {Key}", key);
            return systemConfig;
        }

        _logger.LogDebug("Configuration not found in any scope: {Key}", key);
        return null;
    }

    /// <summary>
    /// Build composite key for storage: "{scope}:{scopeId}:{key}"
    /// 构建存储用的复合键："{scope}:{scopeId}:{key}"
    /// </summary>
    private static string BuildCompositeKey(ConfigurationScope scope, string scopeId, string key)
    {
        return $"{scope}:{scopeId}:{key}";
    }
}
