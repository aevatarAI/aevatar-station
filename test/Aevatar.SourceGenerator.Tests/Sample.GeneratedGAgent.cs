using System;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.SourceGenerator.Tests;

[GAgent(nameof(TestMyArtifact))]
public class TestMyArtifactGAgent : GAgentBase<GeneratedGAgentState, GeneratedStateLogEvent>
{
    private readonly TestMyArtifact _artifact;

    public TestMyArtifactGAgent(ILogger<TestMyArtifactGAgent> logger, TestMyArtifact artifact) : base(logger)
    {
        _artifact = artifact;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(_artifact.GetDescription());
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);
        await UpdateObserverList(_artifact.GetType());
    }

    protected override void GAgentTransitionState(GeneratedGAgentState state, StateLogEventBase<GeneratedStateLogEvent> @event)
    {
        base.GAgentTransitionState(state, @event);
        _artifact.TransitionState(state, @event);
    }
}