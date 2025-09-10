using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;

namespace Aevatar.GAgents.SemanticKernel.ExtractContent;

public interface IExtractContent
{
    public Task<List<ExtractResult>> Extract(BrainContent brainContent, CancellationToken cancellationToken);
}