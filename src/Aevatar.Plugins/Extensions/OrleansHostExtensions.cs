using System.Reflection;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Metadata;
using Orleans.Runtime.Hosting;
using Orleans.Serialization;
using Orleans.Storage;

namespace Aevatar.Plugins.Extensions;

public static class OrleansHostExtensions
{
    public static ISiloBuilder UseAevatarPlugins(this ISiloBuilder siloBuilder)
    {
        return siloBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<ApplicationPartManager>();
            services.AddSingleton<PluginGAgentManager>();
            services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>>(sp =>
                sp.GetRequiredService<PluginGAgentManager>());

            // var assemblies = new TestManager(services.GetRequiredService<IGrainStorage>()).GetAssemblies("test".ToGuid()).Result;
            // foreach (var assembly in assemblies)
            // {
            //     services.AddAssembly(assembly);
            // }
            //
            // services.AddAssembly(typeof(IGAgent).Assembly);
            //
            // services.Configure<GrainTypeOptions>(options =>
            // {
            //     foreach (var assembly in assemblies)
            //     {
            //         foreach (var type in assembly.GetExportedTypes()
            //                      .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IGAgent).IsAssignableFrom(t)))
            //         {
            //             options.Classes.Add(type);
            //         }
            //     }
            // });
            //
            // AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            // {
            //     var assemblyName = new AssemblyName(args.Name);
            //     var assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib", $"{assemblyName.Name}.dll");
            //     return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
            // };
            //
            // services.AddSerializer(options =>
            // {
            //     foreach (var assembly in assemblies)
            //     {
            //         options.AddAssembly(assembly);
            //     }
            // });
        });
    }

    public static IClientBuilder UseAevatarPlugins(this IClientBuilder clientBuilder)
    {
        return clientBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<ApplicationPartManager>();
            services.AddSingleton<IPluginGAgentManager, PluginGAgentManager>();
        });
    }
}