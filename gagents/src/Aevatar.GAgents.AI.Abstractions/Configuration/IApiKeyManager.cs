using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.GAgents.AI.Abstractions.Configuration;

/// <summary>
/// API Key management interface for secure key storage and retrieval
/// API密钥管理接口，用于安全的密钥存储和检索
/// </summary>
public interface IApiKeyManager
{
    /// <summary>
    /// Get API key by name, scope, and scope ID
    /// 通过名称、作用域和作用域ID获取API密钥
    /// </summary>
    /// <param name="keyName">API key name</param>
    /// <param name="scope">Configuration scope</param>
    /// <param name="scopeId">Scope identifier</param>
    /// <returns>API key value or null if not found</returns>
    Task<string?> GetKeyAsync(string keyName, ConfigurationScope scope, string scopeId);

    /// <summary>
    /// Set API key by name, scope, and scope ID
    /// 通过名称、作用域和作用域ID设置API密钥
    /// </summary>
    /// <param name="keyName">API key name</param>
    /// <param name="keyValue">API key value</param>
    /// <param name="scope">Configuration scope</param>
    /// <param name="scopeId">Scope identifier</param>
    Task SetKeyAsync(string keyName, string keyValue, ConfigurationScope scope, string scopeId);

    /// <summary>
    /// Delete API key by name, scope, and scope ID
    /// 通过名称、作用域和作用域ID删除API密钥
    /// </summary>
    /// <param name="keyName">API key name</param>
    /// <param name="scope">Configuration scope</param>
    /// <param name="scopeId">Scope identifier</param>
    Task DeleteKeyAsync(string keyName, ConfigurationScope scope, string scopeId);

    /// <summary>
    /// Get all API key names in the specified scope (without values for security)
    /// 获取指定作用域中的所有API密钥名称（出于安全考虑不包含值）
    /// </summary>
    /// <param name="scope">Configuration scope</param>
    /// <param name="scopeId">Scope identifier</param>
    /// <param name="includeValues">Whether to include key values (default: false for security)</param>
    /// <returns>Dictionary of key names and values (if includeValues is true)</returns>
    Task<Dictionary<string, string?>> GetAllKeysAsync(ConfigurationScope scope, string scopeId, bool includeValues = false);

    /// <summary>
    /// Get API key with cascade resolution (Workflow → Project → User → System)
    /// 使用级联解析获取API密钥（工作流 → 项目 → 用户 → 系统）
    /// </summary>
    /// <param name="keyName">API key name</param>
    /// <param name="workflowId">Workflow ID (optional)</param>
    /// <param name="projectId">Project ID (optional)</param>
    /// <param name="userId">User ID (optional)</param>
    /// <returns>API key value from the highest priority scope that has it</returns>
    Task<string?> GetKeyWithCascadeAsync(string keyName, string? workflowId = null, string? projectId = null, string? userId = null);

    /// <summary>
    /// Check if API key exists
    /// 检查API密钥是否存在
    /// </summary>
    /// <param name="keyName">API key name</param>
    /// <param name="scope">Configuration scope</param>
    /// <param name="scopeId">Scope identifier</param>
    /// <returns>True if key exists</returns>
    Task<bool> KeyExistsAsync(string keyName, ConfigurationScope scope, string scopeId);
}
