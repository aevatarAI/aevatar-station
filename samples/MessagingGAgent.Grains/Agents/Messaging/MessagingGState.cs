using Aevatar.Core.Abstractions;

namespace MessagingGAgent.Grains.Agents.Messaging;

public class MessagingGState : StateBase
{
    public int ReceivedMessages { get; set; } = 0;
    
    public void Apply(MessagingStateLogEvent message)
    {
        ReceivedMessages++;
    }
}