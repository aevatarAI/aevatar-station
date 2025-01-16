using Aevatar.Application.Grains;
using Aevatar.CQRS;
using Aevatar.Kubernetes;
using Aevatar.Options;
using Aevatar.WebHook.Deploy;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.Mvc.Dapr;
using Volo.Abp.AutoMapper;
using Volo.Abp.Dapr;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;

namespace Aevatar;

[DependsOn(
    typeof(AevatarDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(AevatarApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpDaprModule),
    typeof(AbpAspNetCoreMvcDaprModule),
    typeof(AIApplicationGrainsModule),
    typeof(AevatarCQRSModule),
    typeof(AevatarWebhookDeployModule),
    typeof(AevatarKubernetesModule)
)]
public class AevatarApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarApplicationModule>();
        });
        
        var configuration = context.Services.GetConfiguration();
        Configure<WebhookDeployOptions>(configuration.GetSection("WebhookDeploy"));

    }
}
