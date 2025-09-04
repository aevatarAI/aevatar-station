using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GroupChat.Core.Dto;

[GenerateSerializer]
public class GroupMemberConfigDto : ConfigurationBase
{
    [Id(0)] 
    [Description("The name of the group member agent, used for identification and display purposes")]
    public string MemberName { get; set; }
}