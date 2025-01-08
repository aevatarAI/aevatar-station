using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AI.Common;
using Aevatar.AI.KernelBuilderFactory;
using Aevatar.AI.Options;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Aevatar.AI.Brain;

public class AzureOpenAIBrain : IBrain
{
    private Kernel _kernel;
    private string _promptTemplate;
    
    private readonly IOptions<AzureOpenAIConfig> _azureOpenAIConfig;
    private readonly IKernelBuilderFactory _kernelBuilderFactory;
    private readonly AzureOpenAIClient _azureOpenAIClient;

    public AzureOpenAIBrain(IOptions<AzureOpenAIConfig> azureOpenAIConfig, AzureOpenAIClient azureOpenAIClient, IKernelBuilderFactory kernelBuilderFactory)
    {
        _azureOpenAIClient = azureOpenAIClient;
        _kernelBuilderFactory = kernelBuilderFactory;
        _azureOpenAIConfig = azureOpenAIConfig;
    }
    
    public bool Initialize(Guid guid, string promptTemplate, List<File> files)
    {
        var kernelBuilder = _kernelBuilderFactory.GetKernelBuilder(guid);
        kernelBuilder.AddAzureOpenAIChatCompletion(_azureOpenAIConfig.Value.ChatDeploymentName, _azureOpenAIClient);
        _kernel = kernelBuilder.Build();

        var ts = _kernel.GetRequiredService<ITextSearch>();
        ts.CreateWithGetTextSearchResults("SearchPlugin");
        
        //_kernel.Plugins.Add(vectorStoreTextSearch.CreateWithGetTextSearchResults("SearchPlugin"));
        
        _promptTemplate = promptTemplate;

        return true;
    }

    //TODO: add an overloaded function that takes in dictionary to pass arguments to the prompt template
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