using Aevatar.AuthServer.Grants.Options;
using Aevatar.AuthServer.Grants.Providers;
using Aevatar.OpenIddict;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.IdGenerators;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using Volo.Abp.Threading;
using IdentityRole = Volo.Abp.Identity.IdentityRole;

namespace Aevatar.AuthServer.Grants;

[DependsOn(
    typeof(AevatarDomainModule),
    typeof(AevatarApplicationContractsModule),
    typeof(AbpIdentityAspNetCoreModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpOpenIddictAspNetCoreModule)
)]
public class AevatarAuthServerGrantsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        context.Services.Configure<SignatureGrantOptions>(configuration.GetSection("Signature"));
        context.Services.Configure<ChainOptions>(configuration.GetSection("Chains"));
        context.Services.Configure<AppleOptions>(configuration.GetSection("Apple"));
        
        context.Services.AddOptions<AbpOpenIddictExtensionGrantsOptions>()
            .Configure<IServiceProvider>((options, serviceProvider) =>
            {
                options.Grants.Add(GrantTypeConstants.SIGNATURE,
                    new SignatureGrantHandler(context.Services.GetRequiredService<IWalletLoginProvider>(),
                        context.Services.GetRequiredService<ILogger<SignatureGrantHandler>>()));
                options.Grants.Add(GrantTypeConstants.GOOGLE, 
                    new GoogleGrantHandler(context.Services.GetRequiredService<IGoogleProvider>(),
                        context.Services.GetRequiredService<ILogger<GoogleGrantHandler>>()));
                options.Grants.Add(GrantTypeConstants.APPLE, 
                    new AppleGrantHandler(context.Services.GetRequiredService<IAppleProvider>(),
                        context.Services.GetRequiredService<ILogger<AppleGrantHandler>>()));
                options.Grants.Add(GrantTypeConstants.Github, 
                    new GithubGrantHandler(context.Services.GetRequiredService<IGithubProvider>(),
                        context.Services.GetRequiredService<ILogger<GithubGrantHandler>>()));
            });
    }
} 