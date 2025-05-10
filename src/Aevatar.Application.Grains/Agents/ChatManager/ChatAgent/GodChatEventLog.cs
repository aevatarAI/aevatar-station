using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.ChatManager.Chat;

[GenerateSerializer]
public class GodChatEventLog : StateLogEventBase<GodChatEventLog>
{
}

[GenerateSerializer]
public class UpdateUserProfileGodChatEventLog : GodChatEventLog
{
    [Id(0)] public string Gender { get; set; }
    [Id(1)] public DateTime BirthDate { get; set; }
    [Id(2)] public string BirthPlace { get; set; }
    [Id(3)] public string FullName { get; set; }
}

[GenerateSerializer]
public class RenameChatTitleEventLog : GodChatEventLog
{
    [Id(0)] public string Title { get; set; }
}

[GenerateSerializer]
public class SetChatManagerGuidEventLog : GodChatEventLog
{
    [Id(0)] public Guid ChatManagerGuid { get; set; }
}

[GenerateSerializer]
[Obsolete("This class is deprecated and no longer in use.")]
public class SetAIAgentIdLogEvent : GodChatEventLog
{
    [Id(0)] public List<Guid> AIAgentIds { get; set; }
}

[GenerateSerializer]
public class UpdateRegionProxiesLogEvent : GodChatEventLog
{
    [Id(0)] public Dictionary<string, List<Guid>> RegionProxies;
}