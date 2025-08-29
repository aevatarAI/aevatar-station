using System;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.Common;
using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.SemanticKernel.Embeddings;

public class AzureOpenAITextEmbedding : IEmbedding
{
    private readonly IOptions<AzureOpenAIEmbeddingsConfig> _config;
    private readonly AzureOpenAIClient _client;

    public AzureOpenAITextEmbedding(IOptions<AzureOpenAIEmbeddingsConfig> config, IServiceProvider serviceProvider)
    {
        _config = config;
        _client = serviceProvider.GetRequiredKeyedService<AzureOpenAIClient>(AevatarAISemanticKernelConstants.EmbeddingClientServiceKey);
    }

    public void Configure(IKernelBuilder kernelBuilder)
    {
        var config = _config.Value;
        
        kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
            config.DeploymentName,
            _client);
    }
}