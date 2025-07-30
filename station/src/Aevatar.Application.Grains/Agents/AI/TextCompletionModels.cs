using System.Collections.Generic;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// 文本补全结果 - 简化版，只返回5个补全字符串
/// </summary>
[GenerateSerializer]
public class TextCompletionResult
{
    /// <summary>
    /// 5个不同的补全文本
    /// </summary>
    public List<string> Completions { get; set; } = new();
} 