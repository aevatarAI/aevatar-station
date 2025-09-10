using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Common;

namespace GroupChat.GAgent.Feature.Coordinator.GEvent;

[GenerateSerializer]
public class GroupChatFinishEvent : EventBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
}

[GenerateSerializer]
public class EvaluationInterestEvent : EventBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public long ChatTerm { get; set; }
}

[GenerateSerializer]
public class EvaluationInterestResponseEvent : EventBase
{
    [Id(0)] public Guid MemberId { get; set; }
    [Id(1)] public Guid BlackboardId { get; set; }
    [Id(2)] public int InterestValue { get; set; }
    [Id(3)] public long ChatTerm { get; set; }
}

[GenerateSerializer]
public class CoordinatorPingEvent : EventBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
}

[GenerateSerializer]
public class CoordinatorPongEvent : EventBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public Guid MemberId { get; set; }
    [Id(2)] public string MemberName { get; set; }
}

[GenerateSerializer]
public class ChatEvent : EventBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public Guid Speaker { get; set; }
    [Id(3)] public long Term { get; set; }
    [Id(4)] public List<ChatMessage>? CoordinatorMessages { get; set; } = null;
}

[GenerateSerializer]
public class ChatResponseEvent : EventBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public Guid MemberId { get; set; }
    [Id(2)] public string MemberName { get; set; }
    [Id(3)] public ChatResponse ChatResponse { get; set; }
    [Id(4)] public long Term { get; set; }
}

[GenerateSerializer]
public class CoordinatorConfirmChatResponse : EventBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public Guid MemberId { get; set; }
    [Id(2)] public string MemberName { get; set; }
    [Id(3)] public ChatResponse ChatResponse { get; set; }
}