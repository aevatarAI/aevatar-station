using Microsoft.Extensions.Logging;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    [EventHandler]
    public async Task HandleSendConfigEventAsync(AgentConfigEvent @event)
    {
        Logger.LogInformation("SendConfigEvent: {Task}", @event.Configuration.Model.ModelId);
        RaiseEvent(new UpdateSendConfigEvent()
        {
            Event = @event
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleUserMessageEventAsync(UserMessageEvent @event)
    {
        Logger.LogInformation("{Message}", @event);
        if (@event.TargetAgentId != this.GetGrainId().ToString())
        {
            // Not for me
            return;
        }

        if (!_receivedMessageIds.Add(@event.UniqueId))
        {
            return;
        }

        RaiseEvent(new ReceiveUserMessageEvent
        {
            Event = @event
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleAgentMessageEventAsync(AgentMessageEvent @event)
    {
        if (@event.TargetAgentId != this.GetGrainId().ToString())
        {
            // Not for me
            return;
        }

        if (_receivedMessageIds.Contains(@event.UniqueId))
        {
            return;
        }

        _receivedMessageIds.Add(@event.UniqueId);

        RaiseEvent(new ReceiveAgentMessageEvent()
        {
            Event = @event
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleSelfReportEventAsync(SelfReportEvent @event)
    {
        if (@event.TargetAgentId != this.GetGrainId().ToString())
        {
            // Not for me
            return;
        }

        if (_receivedMessageIds.Contains(@event.UniqueId))
        {
            return;
        }

        _receivedMessageIds.Add(@event.UniqueId);

        RaiseEvent(new UpdateChildEvent()
        {
            LastChildDescriptor = @event.SelfReport
        });
        await ConfirmEvents();
    }
}