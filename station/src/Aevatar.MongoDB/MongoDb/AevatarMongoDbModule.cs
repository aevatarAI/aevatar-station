using Aevatar.ApiKey;
using Aevatar.ApiKeys;
using Aevatar.Notification;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AuditLogging.MongoDB;
using Volo.Abp.BackgroundJobs.MongoDB;
using Volo.Abp.FeatureManagement.MongoDB;
using Volo.Abp.Identity.MongoDB;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.MongoDB;
using Volo.Abp.PermissionManagement.MongoDB;
using Volo.Abp.SettingManagement.MongoDB;
using Volo.Abp.Uow;

namespace Aevatar.MongoDB;

[DependsOn(
    typeof(AevatarDomainModule),
    typeof(AbpPermissionManagementMongoDbModule),
    typeof(AbpSettingManagementMongoDbModule),
    typeof(AbpIdentityMongoDbModule),
    typeof(AbpOpenIddictMongoDbModule),
    typeof(AbpAuditLoggingMongoDbModule),
    typeof(AbpFeatureManagementMongoDbModule),
    typeof(AbpBackgroundJobsMongoDbModule)
)]
public class AevatarMongoDbModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        //Example only, remove if not needed
        context.Services.AddMongoDbContext<AevatarMongoDbContext>(options => { options.AddDefaultRepositories(); });

        context.Services.AddTransient<IProjectAppIdRepository, ProjectAppIdMongoRepository>();
        context.Services.AddTransient<INotificationRepository, NotificationMongoRepository>();
        Configure<AbpUnitOfWorkDefaultOptions>(options =>
        {
            // reference: https://abp.io/docs/latest/framework/architecture/domain-driven-design/unit-of-work?_redirected=B8ABF606AA1BDF5C629883DF1061649A#savechangesasync
            options.TransactionBehavior = UnitOfWorkTransactionBehavior.Auto;
        });
    }
}