using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AevatarGodGPTModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarGodGPTModule>();
        });
        
        var configuration = context.Services.GetConfiguration();
    }
}
