using System;
using System.Collections.Generic;
using Orleans;
using Newtonsoft.Json;

namespace Aevatar.GAgents.AI.Common;

/// <summary>
/// Agent详细描述信息结构
/// </summary>
[GenerateSerializer]
public class AgentDescriptionInfo
{
    /// <summary>
    /// Agent唯一标识
    /// </summary>
    [Id(0)]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent显示名称
    /// </summary>
    [Id(1)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent分类 (Social, AI, Blockchain, Trading, Chat, Workflow等)
    /// </summary>
    [Id(2)]
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// L1描述 - 100-150字符快速描述，用于LLM快速匹配
    /// </summary>
    [Id(3)]
    public string L1Description { get; set; } = string.Empty;
    
    /// <summary>
    /// L2描述 - 300-500字符详细能力说明，用于LLM详细理解
    /// </summary>
    [Id(4)]
    public string L2Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 能力列表，用于LLM理解Agent可执行的操作
    /// </summary>
    [Id(5)]
    public List<string> Capabilities { get; set; } = new();
    
    /// <summary>
    /// 标签，便于LLM理解和分类
    /// </summary>
    [Id(6)]
    public List<string> Tags { get; set; } = new();
} 