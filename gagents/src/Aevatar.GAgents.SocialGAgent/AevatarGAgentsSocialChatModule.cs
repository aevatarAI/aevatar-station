using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.SocialChat;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AISmartGAgentSocialGAgentModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AISmartGAgentSocialGAgentModule>();
        });
    }
}