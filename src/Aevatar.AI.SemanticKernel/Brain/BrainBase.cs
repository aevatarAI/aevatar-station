using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI.Common;
using Aevatar.AI.EmbeddedDataLoader;
using Aevatar.AI.KernelBuilderFactory;
using Aevatar.AI.Model;
using Aevatar.AI.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Aevatar.AI.Brain;

public abstract class BrainBase : IBrain
{
    protected Kernel? Kernel;
    protected string? PromptTemplate;
    
    protected readonly IKernelBuilderFactory KernelBuilderFactory;
    protected readonly ILogger Logger;
    protected readonly IOptions<RagConfig> RagConfig;

    protected BrainBase(IKernelBuilderFactory kernelBuilderFactory, ILogger logger, IOptions<RagConfig> ragConfig)
    {
        KernelBuilderFactory = kernelBuilderFactory;
        Logger = logger;
        RagConfig = ragConfig;
    }

    protected abstract Task ConfigureKernelBuilder(IKernelBuilder kernelBuilder);

    public async Task<bool> InitializeAsync(string id, string promptTemplate, List<FileData>? files)
    {
        var kernelBuilder = KernelBuilderFactory.GetKernelBuilder(id);
        await ConfigureKernelBuilder(kernelBuilder);
        Kernel = kernelBuilder.Build();

        if (files != null)
        {
            var ragConfig = RagConfig.Value;
            foreach (var file in files)
            {
                var dataLoader = Kernel.Services.GetKeyedService<IEmbeddedDataLoader>(file.Type);
                if (dataLoader == null)
                {
                    Logger.LogWarning("Data loader not found for file type {FileType}", file.Type);
                    continue;
                }

                await dataLoader.Load(file,
                    ragConfig.DataLoadingBatchSize,
                    ragConfig.DataLoadingBetweenBatchDelayInMilliseconds,
                    new CancellationToken());
            }
        }

        var ts = Kernel.GetRequiredService<VectorStoreTextSearch<TextSnippet<Guid>>>();
        Kernel.Plugins.Add(ts.CreateWithGetTextSearchResults("SearchPlugin"));
        
        PromptTemplate = promptTemplate;

        return true;
    }

    public async Task<string?> InvokePromptAsync(string prompt)
    {
        if(Kernel == null)
        {
            Logger.LogError("Kernel is not initialized, please call InitializeAsync first.");
            return null;
        }
        
        if(PromptTemplate == null)
        {
            Logger.LogError("Prompt template is not set, please call InitializeAsync first.");
            return null;
        }
        
        var result = await Kernel.InvokePromptAsync(
            promptTemplate: PromptTemplate,
            arguments: new KernelArguments()
            {
                { "prompt", prompt },
            },
            templateFormat: AevatarAIConstants.AevatarAITemplateFormat,
            promptTemplateFactory: new HandlebarsPromptTemplateFactory());
        
        return result.GetValue<string>();
    }
} 