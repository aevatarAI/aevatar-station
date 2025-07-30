using System.Collections.Generic;
using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// 文本补全器State - 简化版
/// </summary>
[GenerateSerializer]
public class TextCompletionState : StateBase
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