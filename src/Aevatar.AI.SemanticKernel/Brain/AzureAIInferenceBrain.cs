using System;
using System.Threading.Tasks;
using Aevatar.AI.KernelBuilderFactory;
using Aevatar.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Aevatar.AI.Brain;

public class AzureAIInferenceBrain : BrainBase
{
    private readonly IOptions<AzureAIInferenceConfig> _config;

    public AzureAIInferenceBrain(
        IOptions<AzureAIInferenceConfig> config, 
        IKernelBuilderFactory kernelBuilderFactory, 
        ILogger<AzureAIInferenceBrain> logger, 
        IOptions<RagConfig> ragConfig)
        : base(kernelBuilderFactory, logger, ragConfig)
    {
        _config = config;
    }

    protected override Task ConfigureKernelBuilder(IKernelBuilder kernelBuilder)
    {
        kernelBuilder.AddAzureAIInferenceChatCompletion(
            _config.Value.ChatDeploymentName,
            _config.Value.ApiKey,
            new Uri(_config.Value.Endpoint));
            
        return Task.CompletedTask;
    }
}