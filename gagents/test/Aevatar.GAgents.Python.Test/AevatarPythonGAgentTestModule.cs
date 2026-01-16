using Aevatar.GAgents.TestBase;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Python.Test;

[DependsOn(
    typeof(AevatarGAgentTestBaseModule)
)]
public class AevatarPythonGAgentTestModule : AbpModule;