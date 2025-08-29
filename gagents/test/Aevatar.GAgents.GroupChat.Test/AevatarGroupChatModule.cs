using System.Runtime.CompilerServices;
using Aevatar.GAgents.TestBase;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.GroupChat.Test;

[DependsOn(typeof(AevatarGAgentTestBaseModule))
]
public class AevatarGroupChatModule : AbpModule
{
}