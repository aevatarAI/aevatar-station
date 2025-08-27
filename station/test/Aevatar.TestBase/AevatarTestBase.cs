using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;
using Volo.Abp.Testing;

namespace Aevatar;

/* All test classes are derived from this class, directly or indirectly.
 */
public abstract class AevatarTestBase<TStartupModule> : AbpIntegratedTest<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
    {
        options.UseAutofac();
    }
    
    protected override void BeforeAddApplication(IServiceCollection services)
    {
        var builder = new ConfigurationBuilder();
        
        // 基础配置文件
        builder.AddJsonFile("appsettings.json", optional: false);
        
        // 根据环境变量加载不同的配置
        string env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Testing";
        builder.AddJsonFile($"appsettings.{env}.json", optional: true);
        
        // MongoDB特定配置，只在需要时加载
        if (ShouldUseMongoDB())
        {
            builder.AddJsonFile("appsettings.MongoDB.json", optional: true);
        }
        
        // 秘钥配置
        builder.AddJsonFile("appsettings.secrets.json", optional: true);
        
        // 环境变量
        builder.AddEnvironmentVariables();
        
        services.ReplaceConfiguration(builder.Build());
    }

    /// <summary>
    /// 决定是否应该使用MongoDB配置
    /// 子类可以覆盖此方法来指定是否需要MongoDB
    /// </summary>
    protected virtual bool ShouldUseMongoDB()
    {
        // 检查环境变量是否指定使用MongoDB
        return Environment.GetEnvironmentVariable("USE_MONGODB") == "true";
    }

    protected virtual Task WithUnitOfWorkAsync(Func<Task> func)
    {
        return WithUnitOfWorkAsync(new AbpUnitOfWorkOptions(), func);
    }

    protected virtual async Task WithUnitOfWorkAsync(AbpUnitOfWorkOptions options, Func<Task> action)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            using (var uow = uowManager.Begin(options))
            {
                await action();

                await uow.CompleteAsync();
            }
        }
    }

    protected virtual Task<TResult> WithUnitOfWorkAsync<TResult>(Func<Task<TResult>> func)
    {
        return WithUnitOfWorkAsync(new AbpUnitOfWorkOptions(), func);
    }

    protected virtual async Task<TResult> WithUnitOfWorkAsync<TResult>(AbpUnitOfWorkOptions options, Func<Task<TResult>> func)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            using (var uow = uowManager.Begin(options))
            {
                var result = await func();
                await uow.CompleteAsync();
                return result;
            }
        }
    }
}
