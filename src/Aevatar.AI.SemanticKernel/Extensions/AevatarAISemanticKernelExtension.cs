using System;
using Aevatar.AI.Options;
using Aevatar.AI.VectorStoreBuilder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Qdrant.Client;

namespace Aevatar.AI.Extensions;

public static class AevatarAISemanticKernelExtension
{
    public static IServiceCollection AddQdrantVectorStore(this IServiceCollection services)
    {
        services.AddTransient<Func<IKernelBuilder, IVectorStoreBuilder>>(sp => 
            kernelBuilder => new QdrantVectorStoreBuilder(kernelBuilder));

        // Register Qdrant configuration options
        services.AddOptions<QdrantConfig>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(QdrantConfig.ConfigSectionName).Bind(settings);
            });

        // Register Qdrant client as a singleton
        services.AddSingleton(sp =>
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
}