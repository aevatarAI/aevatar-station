using System;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.Embeddings;
using Aevatar.GAgents.SemanticKernel.VectorStores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;

/// <summary>
/// Factory for creating and configuring Semantic Kernel builders.
/// </summary>
public sealed class KernelBuilderFactory : IKernelBuilderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<RagConfig> _ragConfig;
    
    public KernelBuilderFactory(IServiceProvider serviceProvider, IOptions<RagConfig> ragConfig)
    {
        _serviceProvider = serviceProvider;
        _ragConfig = ragConfig;
    }

    /// <summary>
    /// Creates and configures a kernel builder for the specified GUID.
    /// </summary>
    /// <param name="id">The ID to configure the vector store collection.</param>
    /// <returns>A configured IKernelBuilder instance.</returns>
    /// <exception cref="ArgumentException">Thrown when guid is empty.</exception>
    public IKernelBuilder GetKernelBuilder(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("Brain ID cannot be empty.", nameof(id));
        }

        var config = _ragConfig.Value;

        var kernelBuilder = Kernel.CreateBuilder();
        
        /*var vectorStore = _serviceProvider.GetRequiredKeyedService<IVectorStore>(config.VectorStoreType);
        vectorStore.ConfigureCollection(kernelBuilder, id);
        vectorStore.RegisterVectorStoreTextSearch(kernelBuilder);
        
        var embedding = _serviceProvider.GetRequiredKeyedService<IEmbedding>(config.AIEmbeddingService);
        embedding.Configure(kernelBuilder);*/
        
        return kernelBuilder;
    }
}