namespace GroupChat.GAgent.Feature.Common;

[GenerateSerializer]
public class ChatMessage
{
    [Id(0)] public  MessageType MessageType { get; set; }
    [Id(1)] public Guid MemberId { get; set; }
    [Id(2)] public string AgentName { get; set; }
    [Id(3)] public string Content { get; set; }
}

[GenerateSerializer]
public enum MessageType
{
    User,
    BlackboardTopic
}