using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Volo.Abp.DependencyInjection;

namespace Aevatar.GAgents.AI.Brain;

public interface IBrain : ITransientDependency
{
    LLMProviderEnum ProviderEnum { get; }
    ModelIdEnum ModelIdEnum { get; }

    Task InitializeAsync(LLMConfig llmConfig, string id, string description);

    Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files = null);
}