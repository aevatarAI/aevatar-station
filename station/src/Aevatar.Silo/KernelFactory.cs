using Aevatar.GAgents.PsiOmni.Interfaces;
using Aevatar.GAgents.PsiOmni.Models;
using Microsoft.SemanticKernel;

namespace Aevatar.Silo;

public class KernelFactory : IKernelFactory
{
    public KernelFactory(IKernelFunctionRegistry kernelFunctionRegistry)
    {
        FunctionRegistry = kernelFunctionRegistry;
    }

    public Kernel CreateKernel(AgentConfiguration configuration, IEnumerable<string>? toolNames = null)
    {
        // TODO: validate configuration
        // Create a kernel for LLM analysis (same approach as original)
        var kernelBuilder = Kernel.CreateBuilder();

        // Add chat completion service based on configuration
        if (configuration.Model.IsAzureOpenAI)
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                configuration.Model.DeploymentName ?? configuration.Model.ModelId,
                configuration.Model.Endpoint!,
                configuration.Model.ApiKey);
        }
        else
        {
            kernelBuilder.AddOpenAIChatCompletion(
                configuration.Model.ModelId,
                configuration.Model.ApiKey);
        }


        if (toolNames != null)
        {
            var funcs = new List<KernelFunction>();
            foreach (var toolName in toolNames)
            {
                var func = FunctionRegistry.GetToolByQualifiedName(toolName);
                if (func != null)
                {
                    funcs.Add(func);
                }
            }

            if (funcs.Count > 0)
            {
                var mathPlugin = KernelPluginFactory.CreateFromFunctions("Tools", funcs);
                kernelBuilder.Plugins.Add(mathPlugin);
            }
        }

        return kernelBuilder.Build();
    }

    public IKernelFunctionRegistry? FunctionRegistry { get; }
}