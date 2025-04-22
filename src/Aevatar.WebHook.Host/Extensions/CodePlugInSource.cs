using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Volo.Abp.Modularity;
using Volo.Abp.Modularity.PlugIns;

namespace Aevatar.Webhook.Extensions;

public class CodePlugInSource : IPlugInSource
{
    private readonly Dictionary<string, byte[]> _codeFiles;

    public CodePlugInSource(Dictionary<string, byte[]> codeFiles)
    {
        _codeFiles = codeFiles;
    }

    public Type[] GetModules()
    {
        if (_codeFiles == null || _codeFiles.Count == 0)
        {
            return Array.Empty<Type>();
        }

        var source = new List<Type>();
        var context = new CustomAssemblyLoadContext();
        
        // Load all dlls first
        var assemblies = new Dictionary<string, Assembly>();
        foreach (var file in _codeFiles)
        {
            if (file.Key.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                var assembly = context.LoadFromStream(new MemoryStream(file.Value));
                assemblies[file.Key] = assembly;
            }
        }

        // Then find all modules from loaded assemblies
        foreach (var assembly in assemblies.Values)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (AbpModule.IsAbpModule(type))
                {
                    source.AddIfNotContains(type);
                }
            }
        }

        return source.ToArray();
    }
}

public class CustomAssemblyLoadContext : AssemblyLoadContext
{
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();

    protected override Assembly Load(AssemblyName assemblyName)
    {
        // Check if we've already loaded this assembly
        if (_loadedAssemblies.TryGetValue(assemblyName.Name, out var loadedAssembly))
        {
            return loadedAssembly;
        }

        // Try to load from the default context first
        var assembly = Assembly.Load(assemblyName);
        if (assembly != null)
        {
            _loadedAssemblies[assemblyName.Name] = assembly;
            return assembly;
        }

        // If not found in default context, try to load from the application base directory
        var assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName.Name + ".dll");
        if (File.Exists(assemblyPath))
        {
            assembly = LoadFromAssemblyPath(assemblyPath);
            _loadedAssemblies[assemblyName.Name] = assembly;
            return assembly;
        }

        return null;
    }
}