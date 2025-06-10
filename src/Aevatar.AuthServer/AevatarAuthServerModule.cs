using System.Text;
using Aevatar.Localization;
using Aevatar.MongoDB;
using Aevatar.OpenIddict;
using Aevatar.Options;
using Localization.Resources.AbpUi;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
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
using Volo.Abp.Identity.MongoDB;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.MongoDB;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using Volo.Abp.PermissionManagement;
using Volo.Abp.PermissionManagement.MongoDB;
using Volo.Abp.UI.Navigation.Urls;
using StackExchange.Redis;

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
    typeof(AbpOpenIddictMongoDbModule),
    typeof(AbpIdentityMongoDbModule),
    typeof(AbpPermissionManagementMongoDbModule),
    typeof(AbpAuthorizationModule),
    typeof(AbpOpenIddictDomainModule),
    typeof(AevatarMongoDbModule),
    typeof(AevatarApplicationContractsModule)
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

                options.DisableAccessTokenEncryption();

                int.TryParse(configuration["ExpirationHour"], out int expirationHour);
                if (expirationHour > 0)
                {
                    options.SetAccessTokenLifetime(DateTime.Now.AddHours(expirationHour) - DateTime.Now);
                }

                var encryptionKey = LoadEncryptionKey(configuration);
                options.AddEncryptionKey(encryptionKey);
                Console.WriteLine($"[OpenIddict] 🔑 Loaded encryption key ID: {encryptionKey.KeyId}");
                var signingKey = LoadSigningKey(configuration);
                options.AddSigningKey(signingKey);
                Console.WriteLine($"[OpenIddict] ✍️ Loaded signing key ID: {signingKey.KeyId}");
            });

            builder.AddValidation(options =>
            {
                options.AddAudiences("Aevatar");
                options.UseLocalServer();
                options.UseAspNetCore();

                // Validation不需要额外配置，使用Server的配置
            });
        });

        //add signature grant type
        PreConfigure<OpenIddictServerBuilder>(builder =>
        {
            builder.Configure(openIddictServerOptions =>
            {
                openIddictServerOptions.GrantTypes.Add(GrantTypeConstants.SIGNATURE);
                openIddictServerOptions.GrantTypes.Add(GrantTypeConstants.GOOGLE);
                openIddictServerOptions.GrantTypes.Add(GrantTypeConstants.APPLE);
            });
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        context.Services.Configure<SignatureGrantOptions>(configuration.GetSection("Signature"));
        context.Services.Configure<ChainOptions>(configuration.GetSection("Chains"));
        context.Services.Configure<AppleOptions>(configuration.GetSection("Apple"));
        Configure<AbpMvcLibsOptions>(options => { options.CheckLibs = false; });
        context.Services.Configure<AbpOpenIddictExtensionGrantsOptions>(options =>
        {
            options.Grants.Add(GrantTypeConstants.SIGNATURE, new SignatureGrantHandler());
            options.Grants.Add(GrantTypeConstants.GOOGLE,
                new GoogleGrantHandler(context.Services.GetRequiredService<IConfiguration>(),
                    context.Services.GetRequiredService<ILogger<GoogleGrantHandler>>()));
            options.Grants.Add(GrantTypeConstants.APPLE,
                new AppleGrantHandler(context.Services.GetRequiredService<IConfiguration>(),
                    context.Services.GetRequiredService<ILogger<AppleGrantHandler>>()));
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
                bundle => { bundle.AddFiles("/global-styles.css"); }
            );
        });

        Configure<AbpAuditingOptions>(options =>
        {
            //options.IsEnabledForGetRequests = true;
            options.ApplicationName = "AuthServer";
            options.IsEnabled = false; //Disables the auditing system
        });

        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];

            options.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
        });

        Configure<AbpBackgroundJobOptions>(options => { options.IsJobExecutionEnabled = false; });

        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "Aevatar:"; });
        var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
        context.Services
            .AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, "Aevatar-DataProtection-Keys")
            .SetApplicationName("AevatarAuthServer");

        context.Services.AddHealthChecks();

        Configure<MvcOptions>(options => { options.Conventions.Add(new ApplicationDescription()); });
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

        //app.UseMultiTenancy();

        app.UseUnitOfWork();
        app.UseAuthorization();
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
    }

    private static SecurityKey LoadEncryptionKey(IConfiguration configuration)
    {
        try
        {
            // 使用StringEncryption:DefaultPassPhrase作为种子生成固定密钥
            var passPhrase = configuration["StringEncryption:DefaultPassPhrase"] ?? "DVb2B8QjyeArjCTY";

            using var pbkdf2 = new Rfc2898DeriveBytes(
                passPhrase,
                Encoding.UTF8.GetBytes("aevatar-openiddict-salt"),
                10000,
                HashAlgorithmName.SHA256);

            var keyBytes = pbkdf2.GetBytes(32); // 256 bits
            var key = new SymmetricSecurityKey(keyBytes)
            {
                KeyId = "AEVATAR_FIXED_ENCRYPTION_KEY"
            };

            Console.WriteLine($"[LoadEncryptionKey] ✓ Generated fixed encryption key with ID: {key.KeyId}");
            return key;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoadEncryptionKey] ❌ Failed to load encryption key: {ex.Message}");
            throw;
        }
    }

    private static SecurityKey LoadSigningKey(IConfiguration configuration)
    {
        try
        {
            // 使用StringEncryption:DefaultPassPhrase作为种子生成固定签名密钥
            var passPhrase = configuration["StringEncryption:DefaultPassPhrase"] ?? "DVb2B8QjyeArjCTY";

            // 生成一个256位的HMAC密钥用于签名
            using var pbkdf2 = new Rfc2898DeriveBytes(
                passPhrase,
                Encoding.UTF8.GetBytes("aevatar-signing-salt"),
                10000,
                HashAlgorithmName.SHA256);

            var keyBytes = pbkdf2.GetBytes(32); // 256 bits
            var key = new SymmetricSecurityKey(keyBytes)
            {
                KeyId = "AEVATAR_FIXED_SIGNING_KEY"
            };

            Console.WriteLine($"[LoadSigningKey] ✓ Generated fixed signing key with ID: {key.KeyId}");
            return key;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoadSigningKey] ❌ Failed to load signing key: {ex.Message}");
            throw;
        }
    }
}