using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Aevatar.SignalR;

// ReSharper disable once CheckNamespace
namespace Orleans.Hosting;

public static class ISiloBuilderExtensions
{
    public static ISiloBuilder UseSignalR(this ISiloBuilder builder, Action<SignalROrleansSiloConfigBuilder>? configure = null)
    {
        var cfg = new SignalROrleansSiloConfigBuilder();
        configure?.Invoke(cfg);
        cfg.ConfigureBuilder?.Invoke(builder);
        return builder;
    }

    public static ISiloBuilder RegisterHub<THub>(this ISiloBuilder builder) where THub : Hub
    {
        builder.ConfigureServices(services =>
        {
            services.AddTransient<ILifecycleParticipant<ISiloLifecycle>>(sp =>
                (sp.GetRequiredService<HubLifetimeManager<THub>>() as ILifecycleParticipant<ISiloLifecycle>)!);
        });

        return builder;
    }
}