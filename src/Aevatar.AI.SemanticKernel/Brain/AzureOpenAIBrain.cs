using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI.Common;
using Aevatar.AI.EmbeddedDataLoader;
using Aevatar.AI.KernelBuilderFactory;
using Aevatar.AI.Model;
using Aevatar.AI.Options;
using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AzureOpenAIBrain> _logger;
    private readonly IOptions<RagConfig> _ragConfig;

    public AzureOpenAIBrain(IOptions<AzureOpenAIConfig> azureOpenAIConfig, IKernelBuilderFactory kernelBuilderFactory, IServiceProvider serviceProvider, ILogger<AzureOpenAIBrain> logger, IOptions<RagConfig> ragConfig)
    {
        _kernelBuilderFactory = kernelBuilderFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _ragConfig = ragConfig;
        _azureOpenAIConfig = azureOpenAIConfig;

        _azureOpenAIClient = _serviceProvider.GetRequiredKeyedService<AzureOpenAIClient>(AzureOpenAIConfig.ConfigSectionName);
    }
    
    public async Task<bool> Initialize(string id, string promptTemplate, List<File> files)
    {
        var kernelBuilder = _kernelBuilderFactory.GetKernelBuilder(id);
        kernelBuilder.AddAzureOpenAIChatCompletion(_azureOpenAIConfig.Value.ChatDeploymentName, _azureOpenAIClient);
        _kernel = kernelBuilder.Build();

        var ragConfig = _ragConfig.Value;
        foreach (var file in files)
        {
            var dataLoader = _kernel.Services.GetKeyedService<IEmbeddedDataLoader>(file.Type);
            if (dataLoader == null)
            {
                _logger.LogWarning("Data loader not found for file type {FileType}", file.Type);
                continue;
            }

            await dataLoader.Load(file, 
                ragConfig.DataLoadingBatchSize, 
                ragConfig.DataLoadingBetweenBatchDelayInMilliseconds,
                new CancellationToken());
        }
        
        //VectorStoreTextSearch<TextSnippet<TKey>>
        var ts = _kernel.GetRequiredService<VectorStoreTextSearch<TextSnippet<Guid>>>();
        //ts.CreateWithGetTextSearchResults("SearchPlugin");
        
        _kernel.Plugins.Add(ts.CreateWithGetTextSearchResults("SearchPlugin"));
        
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