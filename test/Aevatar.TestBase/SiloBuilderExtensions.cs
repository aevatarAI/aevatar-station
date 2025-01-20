using System.Reflection;
using Aevatar.Core.Abstractions.Plugin;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.TestBase;

public static class SiloBuilderExtensions
{
    public static ISiloBuilder LoadPluginGAgents(this ISiloBuilder builder)
    {
        return builder.ConfigureServices(services =>
        {
            var pluginDirectoryProviders =
                InterfaceImplementationsFinder.GetImplementations<IPluginDirectoryProvider>();
            var pluginAssemblies = new List<Assembly>();
            foreach (var pluginDirectoryProvider in pluginDirectoryProviders)
            {
                var assemblies = Directory.GetFiles(pluginDirectoryProvider.GetDirectory(), "*.dll")
                    .Select(Assembly.LoadFrom)
                    .ToList();
                pluginAssemblies.AddRange(assemblies);
            }

            var applicationPartManager = new ApplicationPartManager();
            foreach (var assembly in pluginAssemblies)
            {
                applicationPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
            }

            services.AddSingleton(applicationPartManager);
        });
    }
}