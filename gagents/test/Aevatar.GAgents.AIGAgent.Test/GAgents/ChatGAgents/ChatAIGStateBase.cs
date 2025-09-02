using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.State;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;

[GenerateSerializer]
public class ChatAIGStateBase : AIGAgentStateBase
{
    [Id(0)] public List<AIStreamChatContent> ContentList = new List<AIStreamChatContent>();
    [Id(1)] public List<TextToImageResponse> TextToImageResponses = new List<TextToImageResponse>();
    [Id(2)] public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
}