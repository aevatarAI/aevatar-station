using System;
using Aevatar.AuthServer.Grants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;

namespace Aevatar.MongoDB;

[DependsOn(
    typeof(AevatarApplicationTestModule),
    typeof(AevatarMongoDbModule),
    typeof(AevatarAuthServerGrantsTestModule)
)]
public class AevatarMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var useMongoDbFixture = configuration["TestingEnvironment"] != "MongoDB";
        
        Configure<AbpDbConnectionOptions>(options =>
        {
            if (useMongoDbFixture)
            {
                // 使用临时的Mongo2Go实例做单元测试
                options.ConnectionStrings.Default = AevatarMongoDbFixture.GetRandomConnectionString();
            }
            else
            {
                // 使用配置文件中的连接字符串（来自Docker容器）
                var connectionString = configuration.GetConnectionString("Default");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    options.ConnectionStrings.Default = connectionString;
                }
            }
        });
        
        Configure<AbpUnitOfWorkDefaultOptions>(options =>
        {
            options.TransactionBehavior = UnitOfWorkTransactionBehavior.Disabled;
        });
    }
}
