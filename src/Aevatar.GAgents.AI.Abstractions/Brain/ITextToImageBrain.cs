using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AI.Brain;

public interface ITextToImageBrain : IBrain
{
    Task<List<TextToImageResponse>?> GenerateTextToImageAsync(string prompt, TextToImageOption option,
        CancellationToken cancellationToken = default);
}