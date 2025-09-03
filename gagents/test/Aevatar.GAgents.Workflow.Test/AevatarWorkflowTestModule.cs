using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.Extensions;
using Aevatar.GAgents.TestBase;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Workflow.Test;

[DependsOn(
    typeof(AevatarGAgentTestBaseModule)
)]
public class AevatarWorkflowTestModule : AbpModule
{
    
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarWorkflowTestModule>(); });
        context.Services.AddSingleton(new ApplicationPartManager());

        var configuration = context.Services.GetConfiguration();
        Configure<QdrantConfig>(configuration.GetSection("VectorStores:Qdrant"));
        Configure<AzureOpenAIEmbeddingsConfig>(configuration.GetSection("AIServices:AzureOpenAIEmbeddings"));
        Configure<RagConfig>(configuration.GetSection("Rag"));

        context.Services.AddSemanticKernel()
            .AddQdrantVectorStore()
            .AddAzureOpenAITextEmbedding();
    }
}