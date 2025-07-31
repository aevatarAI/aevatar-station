using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// 文本补全器事件 - 简化版
/// </summary>
[GenerateSerializer]
public class TextCompletionEvent : StateLogEventBase<TextCompletionEvent>
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// 输入文本
    /// </summary>
    public string InputText { get; set; } = string.Empty;

    /// <summary>
    /// 补全结果数量
    /// </summary>
    public int CompletionCount { get; set; }
} 