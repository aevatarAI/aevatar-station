using Aevatar.GAgents.MCP;
using Aevatar.GAgents.TestBase;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.MCP.Test;

[DependsOn(
    typeof(AevatarGAgentTestBaseModule),
    typeof(AevatarGAgentsMCPModule)
)]
public class AevatarMCPTestModule : AbpModule;