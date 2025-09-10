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
        var assemblies = new Dictionary<string, Assembly>();

        // 1. 解析依赖关系，构建依赖图
        var dependencyGraph = new Dictionary<string, List<string>>();
        var nameToBytes = new Dictionary<string, byte[]>();
        var nameToFile = new Dictionary<string, string>();
        foreach (var file in _codeFiles)
        {
            if (file.Key.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                using var ms = new MemoryStream(file.Value);
                var asm = Assembly.Load(ms.ToArray());
                var asmName = asm.GetName().Name;
                nameToBytes[asmName] = file.Value;
                nameToFile[asmName] = file.Key;
                var refs = new List<string>();
                foreach (var refAsm in asm.GetReferencedAssemblies())
                {
                    if (nameToBytes.ContainsKey(refAsm.Name) || _codeFiles.ContainsKey(refAsm.Name + ".dll"))
                    {
                        refs.Add(refAsm.Name);
                    }
                }
                dependencyGraph[asmName] = refs;
            }
        }

        // 2. 拓扑排序
        var sorted = new List<string>();
        var visited = new Dictionary<string, int>(); // 0=未访问,1=访问中,2=已完成
        void Visit(string node)
        {
            if (!visited.ContainsKey(node)) visited[node] = 0;
            if (visited[node] == 1)
                throw new Exception($"[ψ依赖环] 检测到循环依赖: {node}");
            if (visited[node] == 2) return;
            visited[node] = 1;
            foreach (var dep in dependencyGraph[node])
            {
                if (!dependencyGraph.ContainsKey(dep))
                    throw new Exception($"[ψ缺失] DLL依赖未找到: {dep} (被{node}依赖)");
                Visit(dep);
            }
            visited[node] = 2;
            sorted.Add(node);
        }
        foreach (var node in dependencyGraph.Keys)
        {
            Visit(node);
        }

        // 3. 按拓扑顺序加载DLL
        foreach (var asmName in sorted)
        {
            var fileKey = nameToFile[asmName];
            var assembly = context.LoadFromStream(new MemoryStream(_codeFiles[fileKey]));
            assemblies[fileKey] = assembly;
        }

        // 4. 查找AbpModule类型
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