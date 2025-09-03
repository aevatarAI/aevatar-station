using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Volo.Abp.BlobStoring;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public sealed class GeminiBrain : BrainBase
{
    public GeminiBrain(
        IKernelBuilderFactory kernelBuilderFactory,
        ILogger<GeminiBrain> logger,
        IOptions<RagConfig> ragConfig,
        IBlobContainer blobContainer)
        : base(kernelBuilderFactory, logger, ragConfig, blobContainer)
    {
    }

    public override LLMProviderEnum ProviderEnum => LLMProviderEnum.Google;
    public override ModelIdEnum ModelIdEnum => ModelIdEnum.Gemini;

    protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
    {
        kernelBuilder.AddGoogleAIGeminiChatCompletion(
            modelId: llmConfig.ModelName,
            apiKey: llmConfig.ApiKey);
        return Task.CompletedTask;
    }

    protected override PromptExecutionSettings GetPromptExecutionSettings(ExecutionPromptSettings promptSettings)
    {
        var result = new GeminiPromptExecutionSettings();
        if (promptSettings.Temperature.IsNullOrWhiteSpace() == false)
        {
            result.Temperature = double.Parse(promptSettings.Temperature);
        }

        if (promptSettings.MaxToken > 0)
        {
            result.MaxTokens = promptSettings.MaxToken;
        }

        return result;
    }

    protected override TokenUsageStatistics GetTokenUsage(IReadOnlyCollection<ChatMessageContent> messageList)
    {
        int inputUsage = 0;
        int outputUsage = 0;
        int totalUsage = 0;
        foreach (var item in messageList)
        {
            if (item is GeminiChatMessageContent { Metadata: not null } completions)
            {
                inputUsage += completions.Metadata.PromptTokenCount;
                outputUsage += completions.Metadata.CurrentCandidateTokenCount;
                totalUsage += completions.Metadata.TotalTokenCount;
            }
        }

        return new TokenUsageStatistics()
        {
            InputToken = inputUsage, OutputToken = outputUsage, TotalUsageToken = totalUsage,
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    public override TokenUsageStatistics GetStreamingTokenUsage(List<object> messageList)
    {
        int inputUsage = 0;
        int outputUsage = 0;
        int totalUsage = 0;
        foreach (var item in messageList)
        {
            if (item is StreamingChatMessageContent streamingChatMessageContent)
            {
                if (streamingChatMessageContent.Metadata != null)
                {
                    var geminiMetadata = streamingChatMessageContent.Metadata as GeminiMetadata;
                    if (geminiMetadata == null)
                    {
                        continue;
                    }

                    inputUsage += geminiMetadata.PromptTokenCount;
                    outputUsage += geminiMetadata.CurrentCandidateTokenCount;
                    totalUsage += geminiMetadata.TotalTokenCount;
                }
            }
        }

        return new TokenUsageStatistics()
        {
            InputToken = inputUsage, OutputToken = outputUsage, TotalUsageToken = totalUsage,
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }
}