using Aevatar.GAgents.PsiOmni.Interfaces;
using Aevatar.GAgents.PsiOmni.Plugins.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.PsiOmni.Plugins;

/// <summary>
/// Registration class for web search kernel functions
/// </summary>
public static class WebSearchFunctionRegistration
{
    /// <summary>
    /// Register all web search functions with the kernel function registry
    /// </summary>
    /// <param name="registry">The kernel function registry</param>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    public static void RegisterAllWebSearchFunctions(
        IKernelFunctionRegistry registry, 
        IServiceProvider serviceProvider)
    {
        var webSearchService = serviceProvider.GetService<IWebSearchService>();
        var webContentFetcher = serviceProvider.GetService<IWebContentFetcher>();
        var logger = serviceProvider.GetService<ILogger<WebSearchPlugin>>();

        if (webSearchService == null)
        {
            logger?.LogWarning("IWebSearchService not registered, skipping web search function registration");
            return;
        }

        if (webContentFetcher == null)
        {
            logger?.LogWarning("IWebContentFetcher not registered, skipping web search function registration");
            return;
        }

        // Create the WebSearch plugin instance
        var webSearchPlugin = new WebSearchPlugin(webSearchService, webContentFetcher, logger!);

        // Create a KernelPlugin from the WebSearchPlugin class
        var kernelPlugin = KernelPluginFactory.CreateFromObject(webSearchPlugin, "WebSearch");

        // Register the WebSearch plugin with all its functions
        registry.RegisterPlugin("WebSearch", kernelPlugin);

        logger?.LogInformation("WebSearch plugin registered successfully with functions: Execute, QuickSearch, FetchContent, FetchMultipleContent");
    }
}