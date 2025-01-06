using System;
using System.Threading.Tasks;
using Aevatar.AI.Common;
using Aevatar.AI.KernelBuilderFactory;
using Aevatar.AI.Options;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Aevatar.AI.Brain;

public class AzureOpenAIBrain : IBrain
{
    private readonly Kernel _kernel;
    private readonly string _promptTemplate;

    public AzureOpenAIBrain(IOptions<AzureOpenAIConfig> config, AzureOpenAIClient azureOpenAIClient, IKernelBuilderFactory kernelBuilderFactory, Guid guid, string promptTemplate)
    {
        var azureOpenAIConfig = config.Value;
        
        var kernelBuilder = kernelBuilderFactory.GetKernelBuilder(guid);
        kernelBuilder.AddAzureOpenAIChatCompletion(azureOpenAIConfig.ChatDeploymentName, azureOpenAIClient);
        _kernel = kernelBuilder.Build();
        
        _promptTemplate = promptTemplate;
    }

    //TODO: added an overloaded function that takes in dictionary to pass arguments to the prompt template
    public async Task<string?> InvokePromptAsync(string prompt)
    {
        var result = await _kernel.InvokePromptAsync(
            promptTemplate: _promptTemplate,
            arguments: new KernelArguments()
            {
                { "prompt", prompt },
            },
            templateFormat: AevatarAIConstants.AevatarAITemplateFormat,
            promptTemplateFactory: new HandlebarsPromptTemplateFactory());
        
        return result.GetValue<string>();
    }
}