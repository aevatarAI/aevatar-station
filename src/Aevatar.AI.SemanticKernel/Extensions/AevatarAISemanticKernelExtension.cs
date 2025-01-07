using System;
using Aevatar.AI.VectorStoreBuilder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Aevatar.AI.Extensions;

public static class AevatarAISemanticKernelExtension
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<Func<IKernelBuilder, IVectorStoreBuilder>>(sp => 
            kernelBuilder => new QdrantVectorStoreBuilder(kernelBuilder));
        
        return services;
    }
}