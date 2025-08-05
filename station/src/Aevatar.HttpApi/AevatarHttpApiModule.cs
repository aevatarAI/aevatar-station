using Aevatar.Application.Grains.Common.Options;
using Aevatar.Application.Contracts.Services;
using Aevatar.Common.Options;
using Aevatar.Core.Abstractions;
using Aevatar.Developer.Logger;
using Localization.Resources.AbpUi;
using Aevatar.Localization;
using Aevatar.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.HttpApi;
using Volo.Abp.AspNetCore.SignalR;

namespace Aevatar;

[DependsOn(
    typeof(AevatarApplicationContractsModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(AevatarDeveloperLoggerModule),
    typeof(AbpAspNetCoreSignalRModule),
    typeof(AevatarGodGPTModule)
    )]
public class AevatarHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureLocalization();

        var configuration = context.Services.GetConfiguration();
        Configure<AevatarOptions>(configuration.GetSection("Aevatar"));
        Configure<StripeOptions>(configuration.GetSection("Stripe"));
        Configure<ManagerOptions>(configuration.GetSection("ManagerOptions"));
        Configure<GodGPTOptions>(configuration.GetSection("GodGPT"));
        Configure<GoogleAnalyticsOptions>(configuration.GetSection("GoogleAnalytics"));
        
        context.Services.AddHttpClient<IGoogleAnalyticsService, Aevatar.Application.Services.GoogleAnalyticsService>();
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<AevatarResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
        });
        
        Configure<MvcOptions>(options =>
        {
            options.Conventions.Add(new ApplicationDescription());
        });
    }
}
