using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI.Brain;

namespace Aevatar.AI.ExtractContent;

public interface IExtractContent
{
    public Task<List<ExtractResult>> Extract(BrainContent brainContent, CancellationToken cancellationToken);
}