using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using Volo.Abp.Modularity;
using Volo.Abp.Modularity.PlugIns;

namespace Aevatar.Webhook.Extensions;

public class CodePlugInSource : IPlugInSource
{
    private byte[] Code { get; }

    public CodePlugInSource(byte[] code)
    {
        Code = code;
    }

    public Type[] GetModules()
    {
        var source = new List<Type>();
        var assembly = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(Code));
        foreach (var type in assembly.GetTypes())
        {
            if (AbpModule.IsAbpModule(type))
            {
                source.AddIfNotContains<Type>(type);
            }
        }

        return source.ToArray();
    }
}