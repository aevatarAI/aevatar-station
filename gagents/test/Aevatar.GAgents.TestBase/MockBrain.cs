using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.Brain;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Volo.Abp.BlobStoring;

namespace Aevatar.GAgents.TestBase;

public class MockBrain: BrainBase
{
    public MockBrain(IKernelBuilderFactory kernelBuilderFactory, ILogger logger, IOptions<RagConfig> ragConfig,
        IBlobContainer blobContainer) : base(kernelBuilderFactory, logger, ragConfig, blobContainer)
    {
    }

    public override LLMProviderEnum ProviderEnum { get; }
    public override ModelIdEnum ModelIdEnum { get; }
    protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
    {
        return Task.CompletedTask;
    }

    protected override PromptExecutionSettings GetPromptExecutionSettings(ExecutionPromptSettings promptSettings)
    {
        return new PromptExecutionSettings();
    }

    protected override TokenUsageStatistics GetTokenUsage(IReadOnlyCollection<ChatMessageContent> messageList)
    {
        return new TokenUsageStatistics();
    }

    public override TokenUsageStatistics GetStreamingTokenUsage(List<object> messageList)
    {
        return new TokenUsageStatistics();
    }
}