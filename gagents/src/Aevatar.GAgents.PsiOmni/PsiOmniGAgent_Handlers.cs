using Microsoft.Extensions.Logging;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    [EventHandler]
    public async Task HandleSendConfigEventAsync(AgentConfigEvent @event)
    {
        await TraceEventHandlerAsync(@event, async () =>
        {
            if (_receivedMessageIds.Contains(@event.UniqueId))
            {
                LogEventDebug("Duplicate config event detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }

            LogEventInfo("SendConfigEvent received: UniqueId={UniqueId}, ModelId={ModelId}",
                @event.UniqueId, @event.Configuration.Model.ModelId);

            if (!_receivedMessageIds.Add(@event.UniqueId))
            {
                LogEventDebug("Duplicate config event detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }

            RaiseEventWithTracing(new UpdateSendConfigEvent()
            {
                Event = @event
            });
            await ConfirmEventsWithTracing();
        });
    }

    [EventHandler]
    public async Task HandleUserMessageEventAsync(UserMessageEvent @event)
    {
        // Check if the message is for this agent
        if (@event.TargetAgentId != this.GetGrainId().ToString())
        {
            // LogEventDebug("Message not for this agent, ignoring");
            return;
        }

        await TraceEventHandlerAsync(@event, async () =>
        {
            LogEventDebug(
                "UserMessageEvent received: UniqueId={UniqueId}, TargetAgentId={TargetAgentId}, CallId={CallId}, Content={Content}",
                @event.UniqueId, @event.TargetAgentId, @event.CallId,
                @event.Content?.Substring(0, Math.Min(@event.Content.Length, 100)));

            if (_receivedMessageIds.Contains(@event.UniqueId))
            {
                LogEventDebug("Duplicate user message event detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }

            if (!_receivedMessageIds.Add(@event.UniqueId))
            {
                LogEventDebug("Duplicate message detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }

            RaiseEventWithTracing(new ReceiveUserMessageEvent
            {
                Event = @event
            });
            await ConfirmEventsWithTracing();
        });
    }

    [EventHandler]
    public async Task HandleAgentMessageEventAsync(AgentMessageEvent @event)
    {
        // Check if the message is for this agent
        if (@event.TargetAgentId != this.GetGrainId().ToString())
        {
            // LogEventDebug("Message not for this agent, ignoring");
            return;
        }

        await TraceEventHandlerAsync(@event, async () =>
        {
            LogEventDebug(
                "AgentMessageEvent received: UniqueId={UniqueId}, TargetAgentId={TargetAgentId}, CallId={CallId}, Content={Content} with {ArtifactCount} artifacts",
                @event.UniqueId, @event.TargetAgentId, @event.CallId,
                @event.Content?.Substring(0, Math.Min(@event.Content.Length, 100)), @event.Artifacts.Count);
            if (_receivedMessageIds.Contains(@event.UniqueId))
            {
                LogEventDebug("Duplicate agent message detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }

            _receivedMessageIds.Add(@event.UniqueId);

            RaiseEventWithTracing(new ReceiveAgentMessageEvent()
            {
                Event = @event
            });
            await ConfirmEventsWithTracing();
        });
    }


    [EventHandler(allowSelfHandling: true)]
    public async Task HandleContinuationEventAsync(ContinuationEvent @event)
    {
        // Check if the message is for this agent
        if (@event.TargetAgentId != this.GetGrainId().ToString())
        {
            return;
        }

        await TraceEventHandlerAsync(@event, async () =>
        {
            LogEventDebug(
                "ContinuationEvent received: UniqueId={UniqueId}, TargetAgentId={TargetAgentId}, ContinuationType={ContinuationType}",
                @event.UniqueId, @event.TargetAgentId, @event.ContinuationType);

            if (_receivedMessageIds.Contains(@event.UniqueId))
            {
                LogEventDebug("Duplicate continuation event detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }

            if (!_receivedMessageIds.Add(@event.UniqueId))
            {
                return;
            }

            switch (@event.ContinuationType)
            {
                case ContinuationType.Initialize:
                    await InitializeAsync();
                    break;
                case ContinuationType.Run:
                    await RunAsync(@event.RunArg);
                    break;
                case ContinuationType.SelfReportAndRun:
                    await DoSelfReportAsync();
                    await RunAsync(@event.RunArg);
                    break;
                case ContinuationType.RegisterAgents:
                    foreach (var newAgent in @event.RegisterAgentIds)
                    {
                        var child = GrainFactory.GetGrain<IGAgent>(GrainId.Parse(newAgent));
                        await RegisterAsync(child);
                    }

                    break;
                case ContinuationType.Retrospect:
                    await RunIntrospectionAsync();
                    break;
                case ContinuationType.IterateOrSelfReportAndReply:
                    var needIteration = false;
                    if (State.IterationCount < 3)
                    {
                        var result = await RunReviewAsync(@event.FinalResponse);
                        LogEventDebug("Review result: IterationCount={IterationCount}, Decision={Decision}, Comment={Comment}", State.IterationCount, result.Decision, result.Comment);
                        needIteration = result.Decision != ReviewDecision.APPROVED;
                        if (needIteration)
                        {
                            RaiseEvent(new IterateEvent()
                            {
                                Comment = result.Comment
                            });
                        }
                    }

                    if (!needIteration)
                    {
                        await DoSelfReportAsync();
                        await ReplyAsync(@event.FinalResponse);
                    }

                    break;
                case ContinuationType.SelfReport:
                    await DoSelfReportAsync();
                    break;
            }
        });
    }


    [EventHandler]
    public async Task HandleSelfReportEventAsync(SelfReportEvent @event)
    {
        // Check if the message is for this agent
        if (@event.TargetAgentId != this.GetGrainId().ToString())
        {
            // LogEventDebug("Self report not for this agent, ignoring");
            return;
        }

        await TraceEventHandlerAsync(@event, async () =>
        {
            LogEventDebug(
                "SelfReportEvent received: UniqueId={UniqueId}, TargetAgentId={TargetAgentId}, ReportingAgent={ReportingAgent}, AgentType={AgentType}",
                @event.UniqueId, @event.TargetAgentId, @event.SelfReport.AgentId, @event.SelfReport.AgentType);

            if (_receivedMessageIds.Contains(@event.UniqueId))
            {
                LogEventDebug("Duplicate self report detected, ignoring: UniqueId={UniqueId}", @event.UniqueId);
                return;
            }

            _receivedMessageIds.Add(@event.UniqueId);

            RaiseEventWithTracing(new UpdateChildEvent()
            {
                LastChildDescriptor = @event.SelfReport
            });
            await ConfirmEventsWithTracing();
        });
    }
}