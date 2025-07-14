using Aevatar.Core.Abstractions;
using Aevatar.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace Aevatar.SignalR.Tests;

[DependsOn(
    typeof(AevatarTestBaseModule),
    typeof(AbpEventBusModule)
)]
public class AevatarSignalRTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Register additional SignalR-specific services
        context.Services.AddTransient<AevatarSignalRHub>();
        context.Services.AddTransient<IAevatarSignalRHub, AevatarSignalRHub>();
    }
}