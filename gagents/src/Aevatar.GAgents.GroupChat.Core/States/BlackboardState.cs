using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Common;

namespace GroupChat.GAgent.Feature.Blackboard.LogEvent;

[GenerateSerializer]
public class BlackboardState : StateBase
{
    [Id(0)] public List<ChatMessage> MessageList = [];
}