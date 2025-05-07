using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.ChatManager;

[GenerateSerializer]
public class ChatManageEventLog : StateLogEventBase<ChatManageEventLog>
{
}

[GenerateSerializer]
public class CreateSessionInfoEventLog : ChatManageEventLog
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string Title { get; set; }
    [Id(2)] public DateTime CreateAt { get; set; }
}

[GenerateSerializer]
public class DeleteSessionEventLog : ChatManageEventLog
{
    [Id(0)] public Guid SessionId { get; set; }
}

[GenerateSerializer]
public class RenameTitleEventLog : ChatManageEventLog
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string Title { get; set; }
}

[GenerateSerializer]
public class ClearAllEventLog : ChatManageEventLog
{
}

[GenerateSerializer]
public class SetUserProfileEventLog : ChatManageEventLog
{
    [Id(0)] public string Gender { get; set; }
    [Id(1)] public DateTime BirthDate { get; set; }
    [Id(2)] public string BirthPlace { get; set; }
    [Id(3)] public string FullName { get; set; }
}

[GenerateSerializer]
public class GenerateChatShareContentLogEvent : ChatManageEventLog
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public Guid ShareId { get; set; }
}

[GenerateSerializer]
public class SetMaxShareCountLogEvent : ChatManageEventLog
{
    [Id(0)] public int MaxShareCount { get; set; }
}