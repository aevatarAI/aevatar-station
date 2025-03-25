using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.SignalR;
using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Orleans.Hosting;

public static class ISignalRBuilderExtensions
{
    public static ISignalRBuilder AddOrleans(this ISignalRBuilder builder)
    {
        builder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));
        var signalRConfig = builder.Services.GetConfiguration().GetSection("AevatarSignalR");
        builder.Services.AddOptions<AevatarSignalROptions>()
            .Bind(signalRConfig)
            .ValidateDataAnnotations();
        var prefix = signalRConfig.GetSection("TopicPrefix").Value ?? builder.Services.GetConfiguration()
            .GetSection("Aevatar").GetSection("StreamNamespace").Value;
        AevatarStreamConfig.Initialize(prefix);
        return builder;
    }

    public static ISignalRServerBuilder AddOrleans(this ISignalRServerBuilder builder)
    {
        builder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));
        var signalRConfig = builder.Services.GetConfiguration().GetSection("AevatarSignalR");
        builder.Services.AddOptions<AevatarSignalROptions>()
            .Bind(signalRConfig)
            .ValidateDataAnnotations();
        var prefix = signalRConfig.GetSection("TopicPrefix").Value ?? builder.Services.GetConfiguration()
            .GetSection("Aevatar").GetSection("StreamNamespace").Value;
        AevatarStreamConfig.Initialize(prefix);
        return builder;
    }
}
