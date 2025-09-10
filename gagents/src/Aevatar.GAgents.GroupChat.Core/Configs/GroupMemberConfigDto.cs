using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GroupChat.Core.Dto;

[GenerateSerializer]
public class GroupMemberConfigDto : ConfigurationBase
{
    [Id(0)] public string MemberName { get; set; }
}