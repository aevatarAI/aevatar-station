using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OpenAI.Chat;
using Volo.Abp.BlobStoring;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public sealed class AzureOpenAIBrain : BrainBase
{
    public override LLMProviderEnum ProviderEnum => LLMProviderEnum.Azure;
    public override ModelIdEnum ModelIdEnum => ModelIdEnum.OpenAI;

    public AzureOpenAIBrain(
        IKernelBuilderFactory kernelBuilderFactory,
        ILogger<AzureOpenAIBrain> logger,
        IOptions<RagConfig> ragConfig,
        IBlobContainer blobContainer)
        : base(kernelBuilderFactory, logger, ragConfig, blobContainer)
    {
    }

    protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
    {
        var clientOptions = new AzureOpenAIClientOptions()
        {
            NetworkTimeout = TimeSpan.FromSeconds(llmConfig.NetworkTimeoutInSeconds)
        };
        
        var azureOpenAi = new AzureOpenAIClient(
            new Uri(llmConfig.Endpoint),
            new AzureKeyCredential(llmConfig.ApiKey),
            clientOptions
        );

        kernelBuilder.AddAzureOpenAIChatCompletion(
            llmConfig.ModelName,
            azureOpenAi);

        return Task.CompletedTask;
    }

    protected override PromptExecutionSettings GetPromptExecutionSettings(ExecutionPromptSettings promptSettings)
    {
        var result = new AzureOpenAIPromptExecutionSettings();
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
            if (item.Metadata != null && item.Metadata.TryGetValue("Usage", out var value))
            {
                var tokenInfo = value as ChatTokenUsage;
                if (tokenInfo == null)
                {
                    continue;
                }

                inputUsage += tokenInfo.InputTokenCount;
                outputUsage += tokenInfo.OutputTokenCount;
                totalUsage += tokenInfo.TotalTokenCount;
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
                if (streamingChatMessageContent.Metadata != null && streamingChatMessageContent.Metadata.TryGetValue("Usage", out var value))
                {
                    var tokenInfo = value as ChatTokenUsage;
                    if (tokenInfo == null)
                    {
                        continue;
                    }

                    inputUsage += tokenInfo.InputTokenCount;
                    outputUsage += tokenInfo.OutputTokenCount;
                    totalUsage += tokenInfo.TotalTokenCount;
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