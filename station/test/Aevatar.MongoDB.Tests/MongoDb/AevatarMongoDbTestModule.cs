using System;
<<<<<<< HEAD
using Volo.Abp.Data;
=======
using Aevatar.AuthServer.Grants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Data;
using Volo.Abp.Identity;
>>>>>>> origin/dev
using Volo.Abp.Modularity;
using Volo.Abp.Uow;

namespace Aevatar.MongoDB;

[DependsOn(
    typeof(AevatarApplicationTestModule),
<<<<<<< HEAD
    typeof(AevatarMongoDbModule)
=======
    typeof(AevatarMongoDbModule),
    typeof(AevatarAuthServerGrantsTestModule)
>>>>>>> origin/dev
)]
public class AevatarMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
<<<<<<< HEAD
        Configure<AbpDbConnectionOptions>(options =>
        {
            options.ConnectionStrings.Default = AevatarMongoDbFixture.GetRandomConnectionString();
=======
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
>>>>>>> origin/dev
        });
        
        Configure<AbpUnitOfWorkDefaultOptions>(options =>
        {
            options.TransactionBehavior = UnitOfWorkTransactionBehavior.Disabled;
        });
    }
}
