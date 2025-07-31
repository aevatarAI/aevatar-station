using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// 文本补全器事件 - 空事件类（无状态服务不需要事件持久化）
/// </summary>
[GenerateSerializer]
public class TextCompletionEvent : StateLogEventBase<TextCompletionEvent>
{
    // 无状态服务，不需要任何事件属性
} 