using System.Collections.Generic;
using Aevatar.GAgents.PsiOmni.Interfaces;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.TestBase.Mocks;

internal sealed class MockKernelFunctionRegistry : IKernelFunctionRegistry
{
    private readonly Dictionary<string, KernelFunction> _functions = new();
    private readonly Dictionary<string, KernelPlugin> _plugins = new();

    public void RegisterFunction(string name, KernelFunction function)
    {
        _functions[name] = function;
    }

    public void RegisterPlugin(string name, KernelPlugin plugin)
    {
        _plugins[name] = plugin;
    }

    public KernelFunction? GetToolByQualifiedName(string qualifiedName)
    {
        _functions.TryGetValue(qualifiedName, out var fn);
        return fn;
    }

    public List<string> GetAllAvailableToolNames()
    {
        return new List<string>(_functions.Keys);
    }
}


