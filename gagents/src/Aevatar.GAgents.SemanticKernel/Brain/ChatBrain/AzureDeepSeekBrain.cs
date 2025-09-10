using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Volo.Abp.BlobStoring;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public sealed class AzureDeepSeekBrain : AzureAIInferenceBrain
{
    public override LLMProviderEnum ProviderEnum => LLMProviderEnum.Azure;
    public override ModelIdEnum ModelIdEnum => ModelIdEnum.DeepSeek;
    
    public AzureDeepSeekBrain(IKernelBuilderFactory kernelBuilderFactory,
        ILogger<AzureAIInferenceBrain> logger, IOptions<RagConfig> ragConfig,
        IBlobContainer blobContainer)
        : base(kernelBuilderFactory, logger, ragConfig, blobContainer)
    {
    }
}