using System;
using Volo.Abp.Data;
using Volo.Abp.Modularity;

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
    }
}
