using Aevatar.MongoDB;
using Aevatar.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Identity;
using Volo.Abp.Identity.MongoDB;
using Volo.Abp.Modularity;
using IdentityRole = Volo.Abp.Identity.IdentityRole;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Aevatar.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AevatarMongoDbModule),
    typeof(AbpIdentityDomainModule),
    typeof(AbpIdentityMongoDbModule),
    typeof(AevatarApplicationContractsModule)
    )]
public class AevatarDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<UsersOptions>(context.Services.GetConfiguration().GetSection("User"));
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "Aevatar:"; });
        IdentityBuilderExtensions.AddDefaultTokenProviders(context.Services.AddIdentity<IdentityUser, IdentityRole>());
        context.Services.AddIdentity<IdentityUser, IdentityRole>()
            .AddDefaultTokenProviders();
    }
}
