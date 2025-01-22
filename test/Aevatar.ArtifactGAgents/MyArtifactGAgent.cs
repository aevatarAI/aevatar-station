using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.ArtifactGAgents;

[GAgent]
public class MyArtifactGAgent : ArtifactGAgentBase<MyArtifact, MyArtifactGAgentState, MyArtifactStateLogEvent>
{
    public MyArtifactGAgent(ILogger<MyArtifactGAgent> logger) : base(logger)
    {
    }
}