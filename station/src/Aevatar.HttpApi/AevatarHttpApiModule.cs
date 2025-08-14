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
        // Debug: Check raw configuration values BEFORE binding
        var securitySection = configuration.GetSection(SecurityOptions.SectionName);
        Console.WriteLine($"[Module Config Debug] Security section exists: {securitySection.Exists()}");
        
        if (securitySection.Exists())
        {
            // Check each subsection and their raw values
            var switchSection = securitySection.GetSection("Switch");
            Console.WriteLine($"[Module Config Debug] Switch section exists: {switchSection.Exists()}");
            
            if (switchSection.Exists())
            {
                var enableRecaptcha = switchSection["EnableRecaptcha"];
                var enableRateLimit = switchSection["EnableRateLimit"];
                Console.WriteLine($"[Module Config Debug] Switch.EnableRecaptcha: '{enableRecaptcha}'");
                Console.WriteLine($"[Module Config Debug] Switch.EnableRateLimit: '{enableRateLimit}'");
            }
            
            var recaptchaSection = securitySection.GetSection("Recaptcha");
            Console.WriteLine($"[Module Config Debug] Recaptcha section exists: {recaptchaSection.Exists()}");
            
            if (recaptchaSection.Exists())
            {
                var secretKey = recaptchaSection["SecretKey"];
                Console.WriteLine($"[Module Config Debug] Recaptcha.SecretKey length: {secretKey?.Length ?? 0}");
            }
            
            // Also check old naming in case server still uses old config
            var oldReCaptchaSection = securitySection.GetSection("ReCAPTCHA");
            if (oldReCaptchaSection.Exists())
            {
                Console.WriteLine($"[Module Config Debug] Found old ReCAPTCHA section - server config needs update!");
            }
            
            if (switchSection.Exists())
            {
                var oldEnableReCAPTCHA = switchSection["EnableReCAPTCHA"];
                if (!string.IsNullOrEmpty(oldEnableReCAPTCHA))
                {
                    Console.WriteLine($"[Module Config Debug] Found old EnableReCAPTCHA: '{oldEnableReCAPTCHA}' - server config needs update!");
                }
            }
        }
        else
        {
            Console.WriteLine("[Module Config Debug] Security section NOT found in configuration!");
        }
        
        // Use standard ABP configuration binding
        context.Services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        
        // Only add validation if needed
        context.Services.AddOptions<SecurityOptions>()
            .Validate(options => 
            {
                // Only validate reCAPTCHA configuration if it's enabled
                if (options.Switch?.EnableRecaptcha == true && string.IsNullOrWhiteSpace(options.Recaptcha?.SecretKey))
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
