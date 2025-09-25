using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Abstractions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Application.Configuration;

/// <summary>
/// In-memory API key manager for development and testing
/// 用于开发和测试的内存API密钥管理器
/// </summary>
public class MemoryApiKeyManager : IApiKeyManager, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, string> _apiKeys;
    private readonly ILogger<MemoryApiKeyManager> _logger;

    public MemoryApiKeyManager(ILogger<MemoryApiKeyManager> logger)
    {
        _apiKeys = new ConcurrentDictionary<string, string>();
        _logger = logger;
    }

    public Task<string?> GetKeyAsync(string keyName, ConfigurationScope scope, string scopeId)
    {
        var compositeKey = BuildCompositeKey(scope, scopeId, keyName);
        
        if (_apiKeys.TryGetValue(compositeKey, out var keyValue))
        {
            _logger.LogDebug("Retrieved API key: {KeyName} from scope {Scope}:{ScopeId}", keyName, scope, scopeId);
            return Task.FromResult<string?>(keyValue);
        }

        _logger.LogDebug("API key not found: {KeyName} in scope {Scope}:{ScopeId}", keyName, scope, scopeId);
        return Task.FromResult<string?>(null);
    }

    public Task SetKeyAsync(string keyName, string keyValue, ConfigurationScope scope, string scopeId)
    {
        var compositeKey = BuildCompositeKey(scope, scopeId, keyName);
        
        _apiKeys.AddOrUpdate(compositeKey, keyValue, (_, _) => keyValue);
        
        _logger.LogDebug("Set API key: {KeyName} in scope {Scope}:{ScopeId}", keyName, scope, scopeId);
        return Task.CompletedTask;
    }

    public Task DeleteKeyAsync(string keyName, ConfigurationScope scope, string scopeId)
    {
        var compositeKey = BuildCompositeKey(scope, scopeId, keyName);
        
        if (_apiKeys.TryRemove(compositeKey, out _))
        {
            _logger.LogDebug("Deleted API key: {KeyName} from scope {Scope}:{ScopeId}", keyName, scope, scopeId);
        }
        else
        {
            _logger.LogDebug("API key not found for deletion: {KeyName} in scope {Scope}:{ScopeId}", keyName, scope, scopeId);
        }

        return Task.CompletedTask;
    }

    public Task<Dictionary<string, string?>> GetAllKeysAsync(ConfigurationScope scope, string scopeId, bool includeValues = false)
    {
        var prefix = BuildCompositeKey(scope, scopeId, "");
        var matchingKeys = _apiKeys
            .Where(kvp => kvp.Key.StartsWith(prefix))
            .ToDictionary(
                kvp => ExtractKeyNameFromCompositeKey(kvp.Key),
                kvp => includeValues ? kvp.Value : null
            );

        _logger.LogDebug("Retrieved {Count} API keys from scope {Scope}:{ScopeId} (includeValues: {IncludeValues})", 
            matchingKeys.Count, scope, scopeId, includeValues);

        return Task.FromResult(matchingKeys);
    }

    public async Task<string?> GetKeyWithCascadeAsync(string keyName, string? workflowId = null, string? projectId = null, string? userId = null)
    {
        // Cascade resolution: Workflow → Project → User → System
        // 级联解析：工作流 → 项目 → 用户 → 系统

        // Try Workflow scope first
        if (!string.IsNullOrEmpty(workflowId))
        {
            var workflowKey = await GetKeyAsync(keyName, ConfigurationScope.Workflow, workflowId);
            if (workflowKey != null)
            {
                _logger.LogDebug("API key resolved from Workflow scope: {KeyName}", keyName);
                return workflowKey;
            }
        }

        // Try Project scope
        if (!string.IsNullOrEmpty(projectId))
        {
            var projectKey = await GetKeyAsync(keyName, ConfigurationScope.Project, projectId);
            if (projectKey != null)
            {
                _logger.LogDebug("API key resolved from Project scope: {KeyName}", keyName);
                return projectKey;
            }
        }

        // Try User scope
        if (!string.IsNullOrEmpty(userId))
        {
            var userKey = await GetKeyAsync(keyName, ConfigurationScope.User, userId);
            if (userKey != null)
            {
                _logger.LogDebug("API key resolved from User scope: {KeyName}", keyName);
                return userKey;
            }
        }

        // Try System scope
        var systemKey = await GetKeyAsync(keyName, ConfigurationScope.System, "global");
        if (systemKey != null)
        {
            _logger.LogDebug("API key resolved from System scope: {KeyName}", keyName);
            return systemKey;
        }

        _logger.LogDebug("API key not found in any scope: {KeyName}", keyName);
        return null;
    }

    public Task<bool> KeyExistsAsync(string keyName, ConfigurationScope scope, string scopeId)
    {
        var compositeKey = BuildCompositeKey(scope, scopeId, keyName);
        var exists = _apiKeys.ContainsKey(compositeKey);
        
        _logger.LogDebug("API key exists check: {KeyName} in scope {Scope}:{ScopeId} = {Exists}", keyName, scope, scopeId, exists);
        return Task.FromResult(exists);
    }

    /// <summary>
    /// Build composite key for storage: "{scope}:{scopeId}:{keyName}"
    /// 构建存储用的复合键："{scope}:{scopeId}:{keyName}"
    /// </summary>
    private static string BuildCompositeKey(ConfigurationScope scope, string scopeId, string keyName)
    {
        return $"{scope}:{scopeId}:{keyName}";
    }

    /// <summary>
    /// Extract key name from composite key
    /// 从复合键中提取密钥名称
    /// </summary>
    private static string ExtractKeyNameFromCompositeKey(string compositeKey)
    {
        var parts = compositeKey.Split(':', 3);
        return parts.Length >= 3 ? parts[2] : compositeKey;
    }
}
