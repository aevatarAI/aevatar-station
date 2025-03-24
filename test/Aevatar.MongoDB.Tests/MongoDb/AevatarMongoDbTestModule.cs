using System;
using Volo.Abp.Data;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;

namespace Aevatar.MongoDB;

[DependsOn(
    typeof(AevatarApplicationTestModule),
    typeof(AevatarMongoDbModule)
)]
public class AevatarMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpDbConnectionOptions>(options =>
        {
            options.ConnectionStrings.Default = AevatarMongoDbFixture.GetRandomConnectionString();
        });
        
        Configure<AbpUnitOfWorkDefaultOptions>(options =>
        {
            options.TransactionBehavior = UnitOfWorkTransactionBehavior.Disabled;
        });
    }
}
