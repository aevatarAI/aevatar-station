namespace Aevatar.GAgents.AI.Abstractions.Configuration;

/// <summary>
/// Configuration scope enumeration for hierarchical configuration management
/// 配置作用域枚举，用于分层配置管理
/// </summary>
public enum ConfigurationScope
{
    /// <summary>
    /// System-wide configuration, applies to all users and workflows
    /// 系统级配置，适用于所有用户和工作流
    /// </summary>
    System = 0,

    /// <summary>
    /// User-specific configuration, applies to a specific user
    /// 用户级配置，适用于特定用户
    /// </summary>
    User = 1,

    /// <summary>
    /// Project/Workspace-level configuration, shared within a project
    /// 项目/工作空间级配置，在项目内共享
    /// </summary>
    Project = 2,

    /// <summary>
    /// Workflow-specific configuration, applies to individual workflows
    /// 工作流级配置，适用于单个工作流
    /// </summary>
    Workflow = 3
}
