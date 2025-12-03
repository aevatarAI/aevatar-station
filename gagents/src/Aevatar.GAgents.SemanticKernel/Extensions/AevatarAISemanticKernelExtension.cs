using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.Brain;
using Aevatar.GAgents.SemanticKernel.Common;
using Aevatar.GAgents.SemanticKernel.Embeddings;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Aevatar.GAgents.SemanticKernel.VectorStores;
using Aevatar.GAgents.SemanticKernel.VectorStores.Qdrant;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qdrant.Client;

namespace Aevatar.GAgents.SemanticKernel.Extensions;

public static class AevatarAISemanticKernelExtension
{
    public static IServiceCollection AddSemanticKernel(this IServiceCollection services)
    {
        services.AddSingleton<IBrainFactory, BrainFactory.BrainFactory>();
        services.AddSingleton<IKernelBuilderFactory, KernelBuilderFactory.KernelBuilderFactory>();

        return services;
    }

    public static IServiceCollection AddQdrantVectorStore(this IServiceCollection services)
    {
        services.AddKeyedTransient<IVectorStore, QdrantVectorStore>(QdrantConfig.ConfigSectionName);
        // Register Qdrant configuration options
        // services.AddOptions<QdrantConfig>()
        //     .Configure<IConfiguration>((settings, configuration) =>
        //     {
        //         configuration.GetSection(QdrantConfig.ConfigSectionName).Bind(settings);
        //     });

        // Register Qdrant client as a singleton
        services.AddSingleton<QdrantClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<QdrantConfig>>().Value;
            return new QdrantClient(
                host: options.Host,
                port: options.Port,
                https: options.Https,
                apiKey: options.ApiKey);
        });

        return services;
    }

    public static IServiceCollection AddAzureOpenAITextEmbedding(this IServiceCollection services)
    {
        // services.AddOptions<AzureOpenAIEmbeddingsConfig>()
        //     .Configure<IConfiguration>((settings, configuration) =>
        //     {
        //         configuration.GetSection(AzureOpenAIEmbeddingsConfig.ConfigSectionName).Bind(settings);
        //     });

        services.AddKeyedSingleton<AzureOpenAIClient>(AevatarAISemanticKernelConstants.EmbeddingClientServiceKey,
            (sp, key) =>
            {
                var options = sp.GetRequiredService<IOptions<AzureOpenAIEmbeddingsConfig>>().Value;
                return new AzureOpenAIClient(
                    new Uri(options.Endpoint),
                    new AzureKeyCredential(options.ApiKey)
                );
            });

        services.AddKeyedTransient<IEmbedding, AzureOpenAITextEmbedding>(AzureOpenAIEmbeddingsConfig.ConfigSectionName);

        // Register AzureOpenAIEmbedding configuration options
        services.AddOptions<AzureOpenAIEmbeddingsConfig>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(AzureOpenAIEmbeddingsConfig.ConfigSectionName).Bind(settings);
            });

        return services;
    }

    private static List<Type> GetAllLLM()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes()
            .Where(t => typeof(IBrain).IsAssignableFrom(t)
                        && t is { IsClass: true, IsAbstract: false })
            .ToList();

        return types;
    }

}