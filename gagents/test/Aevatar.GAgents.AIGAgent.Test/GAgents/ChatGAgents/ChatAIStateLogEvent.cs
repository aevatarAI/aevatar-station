using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;

[GenerateSerializer]
public class ChatAIStateLogEvent : StateLogEventBase<ChatAIStateLogEvent>
{
    
}

[GenerateSerializer]
public class AddMessageLogEvent : ChatAIStateLogEvent
{
    [Id(0)] public AIStreamChatContent Content { get; set; }
}

[GenerateSerializer]
public class TextToImageLogEvent : ChatAIStateLogEvent
{
    [Id(0)] public List<TextToImageResponse> TextToImageResponses { get; set; }
}
