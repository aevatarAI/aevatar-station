using System.Collections.Generic;
using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// 文本补全器State - 继承AI状态基类
/// </summary>
[GenerateSerializer]
public class TextCompletionState : AIGAgentStateBase
{
    /// <summary>
    /// 总补全次数
    /// </summary>
    public int TotalCompletions { get; set; } = 0;

    /// <summary>
    /// 最近的补全历史（最多保留10条）
    /// </summary>
    public List<string> RecentCompletions { get; set; } = new();
} 