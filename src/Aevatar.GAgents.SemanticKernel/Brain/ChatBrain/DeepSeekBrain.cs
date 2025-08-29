using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI;
using OpenAI.Chat;
using Volo.Abp.BlobStoring;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public class DeepSeekBrain : BrainBase
{
    public override LLMProviderEnum ProviderEnum => LLMProviderEnum.DeepSeek;
    public override ModelIdEnum ModelIdEnum => ModelIdEnum.DeepSeek;

    public DeepSeekBrain(IKernelBuilderFactory kernelBuilderFactory, ILogger<DeepSeekBrain> logger, IOptions<RagConfig> ragConfig,
        IBlobContainer blobContainer)
        : base(kernelBuilderFactory, logger, ragConfig, blobContainer)
    {
    }

    protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
    {
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(llmConfig.ApiKey),
            new OpenAIClientOptions() { Endpoint = new Uri(llmConfig.Endpoint) }
        );

        kernelBuilder.AddOpenAIChatCompletion(llmConfig.ModelName, openAiClient);

        return Task.CompletedTask;
    }

    protected override PromptExecutionSettings GetPromptExecutionSettings(ExecutionPromptSettings promptSettings)
    {
        var result = new OpenAIPromptExecutionSettings();
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
            if (item.InnerContent is ChatCompletion completions)
            {
                inputUsage += completions.Usage.InputTokenCount;
                outputUsage += completions.Usage.OutputTokenCount;
                totalUsage += completions.Usage.TotalTokenCount;
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
                if (streamingChatMessageContent.InnerContent is ChatCompletion completions)
                {
                    inputUsage += completions.Usage.InputTokenCount;
                    outputUsage += completions.Usage.OutputTokenCount;
                    totalUsage += completions.Usage.TotalTokenCount;
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