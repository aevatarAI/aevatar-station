using Aevatar.Application.Grains.Common.Options;
using Aevatar.Application.Contracts.Services;
using Aevatar.Common.Options;
using Aevatar.Core.Abstractions;
using Aevatar.Developer.Logger;
using Localization.Resources.AbpUi;
using Aevatar.Localization;
using Aevatar.Options;
using Aevatar.Service;
using Aevatar.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        Configure<FirebaseAnalyticsOptions>(configuration.GetSection("FirebaseAnalytics"));
        
        // Configure security options and services
        ConfigureSecurityOptions(context, configuration);
        ConfigureSecurityServices(context);
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

    private void ConfigureSecurityOptions(ServiceConfigurationContext context, IConfiguration configuration)
    {
        // Configure each security module as separate 2-level configuration
        context.Services.Configure<RecaptchaOptions>(configuration.GetSection(RecaptchaOptions.SectionName));


        context.Services.Configure<RateOptions>(configuration.GetSection(RateOptions.SectionName));
        
        // Add validation for reCAPTCHA configuration
        context.Services.AddOptions<RecaptchaOptions>()
            .Validate(options => 
            {
                // Only validate reCAPTCHA configuration if it's enabled
                if (options.Enabled && string.IsNullOrWhiteSpace(options.SecretKey))
                {
                    return false;
                }
                return true;
            }, "reCAPTCHA SecretKey cannot be empty when reCAPTCHA is enabled");
    }

    private void ConfigureSecurityServices(ServiceConfigurationContext context)
    {
        // Register HTTP client for reCAPTCHA and Firebase verification
        context.Services.AddHttpClient<SecurityService>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Aevatar-Station/1.0");
        });

        // Register distributed cache for rate limiting (use memory cache as fallback)
        context.Services.AddDistributedMemoryCache();

        // Register unified security service
        context.Services.AddTransient<ISecurityService, SecurityService>();
    }
}
