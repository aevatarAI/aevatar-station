using Aevatar.Localization;
using Aevatar.MongoDB;
using Aevatar.OpenIddict;
using Aevatar.Options;
using Localization.Resources.AbpUi;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Mvc.Libs;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Caching;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using Volo.Abp.PermissionManagement;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.UI.Navigation.Urls;

namespace Aevatar.AuthServer;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpOpenIddictEntityFrameworkCoreModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),
    typeof(AbpAuthorizationModule),
    typeof(AbpOpenIddictDomainModule),
    typeof(AevatarMongoDbModule)
    )]
public class AevatarAuthServerModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddServer(options =>
            {
                options.UseAspNetCore().DisableTransportSecurityRequirement();
                options.SetIssuer(new Uri(configuration["AuthServer:IssuerUri"]!));
                // options.IgnoreGrantTypePermissions();
                int.TryParse(configuration["ExpirationHour"], out int expirationHour);
                if (expirationHour > 0)
                {
                    options.SetAccessTokenLifetime(DateTime.Now.AddHours(expirationHour) - DateTime.Now);
                }
            });
            builder.AddValidation(options =>
            {
                options.AddAudiences("Aevatar");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });
        
        //add signature grant type
        PreConfigure<OpenIddictServerBuilder>(builder =>
        {
            builder.Configure(openIddictServerOptions =>
            {
                openIddictServerOptions.GrantTypes.Add(GrantTypeConstants.LOGIN);
                openIddictServerOptions.GrantTypes.Add(GrantTypeConstants.SIGNATURE);
            });
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        context.Services.Configure<SignatureGrantOptions>(configuration.GetSection("Signature"));
        context.Services.Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<AbpMvcLibsOptions>(options =>
        {
            options.CheckLibs = false; 
        });
        context.Services.Configure<AbpOpenIddictExtensionGrantsOptions>(options =>
        {
            options.Grants.Add(GrantTypeConstants.LOGIN, new LoginGrantHandler());
            options.Grants.Add(GrantTypeConstants.SIGNATURE, new SignatureGrantHandler());
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<AevatarResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
            options.Languages.Add(new LanguageInfo("ar", "ar", "العربية"));
            options.Languages.Add(new LanguageInfo("cs", "cs", "Čeština"));
            options.Languages.Add(new LanguageInfo("en", "en", "English"));
            options.Languages.Add(new LanguageInfo("en-GB", "en-GB", "English (UK)"));
            options.Languages.Add(new LanguageInfo("fi", "fi", "Finnish"));
            options.Languages.Add(new LanguageInfo("fr", "fr", "Français"));
            options.Languages.Add(new LanguageInfo("hi", "hi", "Hindi"));
            options.Languages.Add(new LanguageInfo("is", "is", "Icelandic"));
            options.Languages.Add(new LanguageInfo("it", "it", "Italiano"));
            options.Languages.Add(new LanguageInfo("hu", "hu", "Magyar"));
            options.Languages.Add(new LanguageInfo("pt-BR", "pt-BR", "Português"));
            options.Languages.Add(new LanguageInfo("ro-RO", "ro-RO", "Română"));
            options.Languages.Add(new LanguageInfo("ru", "ru", "Русский"));
            options.Languages.Add(new LanguageInfo("sk", "sk", "Slovak"));
            options.Languages.Add(new LanguageInfo("tr", "tr", "Türkçe"));
            options.Languages.Add(new LanguageInfo("zh-Hans", "zh-Hans", "简体中文"));
            options.Languages.Add(new LanguageInfo("zh-Hant", "zh-Hant", "繁體中文"));
            options.Languages.Add(new LanguageInfo("de-DE", "de-DE", "Deutsch"));
            options.Languages.Add(new LanguageInfo("es", "es", "Español"));
            options.Languages.Add(new LanguageInfo("el", "el", "Ελληνικά"));
        });

        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );
        });

        Configure<AbpAuditingOptions>(options =>
        {
                //options.IsEnabledForGetRequests = true;
                options.ApplicationName = "AuthServer";
                options.IsEnabled = false;//Disables the auditing system
        });

        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];

            options.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
        });

        Configure<AbpBackgroundJobOptions>(options =>
        {
            options.IsJobExecutionEnabled = false;
        });

        Configure<AbpDistributedCacheOptions>(options =>
        {
            options.KeyPrefix = ":Auth:";
        });

        var dataProtectionBuilder = context.Services.AddDataProtection().SetApplicationName("AevatarAuthServer");
        
        context.Services.AddHealthChecks();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }
        
        app.UseHealthChecks("/health");

        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        app.UseMultiTenancy();
        
        app.UseUnitOfWork();
        app.UseAuthorization();
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
    }
}
