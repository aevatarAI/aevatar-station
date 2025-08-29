using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;

namespace Aevatar.GAgents.SemanticKernel.ExtractContent;

public class ExtractString:IExtractContent
{
    public Task<List<ExtractResult>> Extract(BrainContent brainContent, CancellationToken cancellationToken)
    {
        return Task.FromResult<List<ExtractResult>>([
            new ExtractResult()
                { Name = brainContent.Name, Content = BrainContent.ConvertBytesToString(brainContent.Content) }
        ]);
    }
}