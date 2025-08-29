
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Common;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AevatarGAgentsCommonModule:AbpModule
{
    
}