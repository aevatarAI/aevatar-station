using System.Collections.Generic;

namespace Aevatar.Agent;

/// <summary>
/// Agent项目DTO (对齐现有结构)
/// </summary>
public class AgentItemDto
{
    /// <summary>
    /// Agent唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent类型 (直接使用原始AgentType)
    /// </summary>
    public string AgentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent属性信息
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }
    
    /// <summary>
    /// 业务Agent Grain ID
    /// </summary>
    public string? BusinessAgentGrainId { get; set; }
    
    /// <summary>
    /// Agent描述 (从Properties中提取)
    /// </summary>
    public string? Description { get; set; }
} 