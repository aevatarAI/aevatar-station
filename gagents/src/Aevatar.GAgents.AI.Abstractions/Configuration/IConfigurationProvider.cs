using System.Threading.Tasks;

namespace Aevatar.GAgents.AI.Abstractions.Configuration;

/// <summary>
/// Generic configuration provider interface for dynamic configuration management
/// 通用配置提供商接口，用于动态配置管理
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Get configuration value by key, scope, and scope ID
    /// 通过键、作用域和作用域ID获取配置值
    /// </summary>
    /// <typeparam name="T">Configuration type</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="scope">Configuration scope</param>
    /// <param name="scopeId">Scope identifier (user ID, project ID, workflow ID, etc.)</param>
    /// <returns>Configuration value or null if not found</returns>
    Task<T?> GetConfigurationAsync<T>(string key, ConfigurationScope scope, string scopeId) where T : class;

    /// <summary>
    /// Set configuration value by key, scope, and scope ID
    /// 通过键、作用域和作用域ID设置配置值
    /// </summary>
    /// <typeparam name="T">Configuration type</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="configuration">Configuration value</param>
    /// <param name="scope">Configuration scope</param>
    /// <param name="scopeId">Scope identifier</param>
    Task SetConfigurationAsync<T>(string key, T configuration, ConfigurationScope scope, string scopeId) where T : class;

    /// <summary>
    /// Delete configuration by key, scope, and scope ID
    /// 通过键、作用域和作用域ID删除配置
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="scope">Configuration scope</param>
    /// <param name="scopeId">Scope identifier</param>
    Task DeleteConfigurationAsync(string key, ConfigurationScope scope, string scopeId);

    /// <summary>
    /// Check if configuration exists
    /// 检查配置是否存在
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="scope">Configuration scope</param>
    /// <param name="scopeId">Scope identifier</param>
    /// <returns>True if configuration exists</returns>
    Task<bool> ExistsAsync(string key, ConfigurationScope scope, string scopeId);

    /// <summary>
    /// Get configuration with cascade resolution (Workflow → Project → User → System)
    /// 使用级联解析获取配置（工作流 → 项目 → 用户 → 系统）
    /// </summary>
    /// <typeparam name="T">Configuration type</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="workflowId">Workflow ID (optional)</param>
    /// <param name="projectId">Project ID (optional)</param>
    /// <param name="userId">User ID (optional)</param>
    /// <returns>Configuration value from the highest priority scope that has it</returns>
    Task<T?> GetConfigurationWithCascadeAsync<T>(string key, string? workflowId = null, string? projectId = null, string? userId = null) where T : class;
}
