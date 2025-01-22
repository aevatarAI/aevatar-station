using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Plugins.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.Extensions;

public static class OrleansHostExtensions
{
    public static ISiloBuilder UseAevatar(this ISiloBuilder builder)
    {
        return builder.ConfigureServices(services =>
            {
                services.AddSingleton<IGAgentManager, GAgentManager>();
                services.AddSingleton<IGAgentFactory, GAgentFactory>();
            })
            .UseAevatarPlugins();
    }
}