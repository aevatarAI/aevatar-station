// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using Aevatar.Core;
// using Aevatar.Core.Abstractions;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
//
// namespace Aevatar.GAgents.MyArtifactGAgent;
//
// public interface IMyArtifactGAgent1 : IGAgent;
//
// public partial class MyArtifactGAgent1 : GAgentBase<GeneratedGAgentState, GeneratedStateLogEvent>, IMyArtifactGAgent1;
//
//
// [GAgent("myartifact")]
// public partial class MyArtifactGAgent1 : IArtifactGAgent
// {
//     private readonly IMyArtifact _artifact;
//
//     public MyArtifactGAgent1(ILogger<MyArtifactGAgent1> logger) : base(logger)
//     {
//         _artifact = ActivatorUtilities.CreateInstance<MyArtifact>(ServiceProvider);
//     }
//
//     public override Task<string> GetDescriptionAsync()
//     {
//         return Task.FromResult(_artifact.GetDescription());
//     }
//
//     protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
//     {
//         await base.OnGAgentActivateAsync(cancellationToken);
//         await UpdateObserverList(_artifact.GetType());
//     }
//
//     protected override void GAgentTransitionState(Aevatar.GAgents.GeneratedGAgentState state, StateLogEventBase<Aevatar.GAgents.GeneratedStateLogEvent> @event)
//     {
//         base.GAgentTransitionState(state, @event);
//         _artifact.TransitionState(state, @event);
//     }
// }