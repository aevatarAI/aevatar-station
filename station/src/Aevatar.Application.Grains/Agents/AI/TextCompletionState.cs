using System.Collections.Generic;
using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.Application.Grains.Agents.AI;

/// <summary>
/// 文本补全器State - 继承AI状态基类，极简版本
/// </summary>
[GenerateSerializer]
public class TextCompletionState : AIGAgentStateBase
{
    // 不需要保存任何状态，只继承基类即可
} 