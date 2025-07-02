using Aevatar.AuthServer.Grants.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using IdentityRole = Volo.Abp.Identity.IdentityRole;

namespace Aevatar.AuthServer.Grants;

[DependsOn(
    typeof(AevatarAuthServerGrantsModule),
    typeof(AevatarTestBaseModule),
    typeof(AbpAutofacModule)
)]
public class AevatarAuthServerGrantsTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AppleOptions>(options =>
        {
            options.APPs = new Dictionary<string, AppleAppOptions> { { "com.gpt.god", new AppleAppOptions() } };
        });
    }
    
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        AsyncHelper.RunSync(async () => await InitBasicRoleAsync(context));
    }

    private async Task InitBasicRoleAsync(ApplicationInitializationContext context)
    {
        var roleManager = context.ServiceProvider.GetRequiredService<IdentityRoleManager>();
        var role = new IdentityRole(
            Guid.NewGuid(),
            Permissions.AevatarPermissions.BasicUser);
        (await roleManager.CreateAsync(role)).CheckErrors();
    }
} 