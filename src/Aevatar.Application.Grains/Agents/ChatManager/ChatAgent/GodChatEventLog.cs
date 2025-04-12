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
}