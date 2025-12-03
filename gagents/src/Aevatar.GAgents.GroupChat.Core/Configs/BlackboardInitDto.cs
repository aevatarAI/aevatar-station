using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.Feature.Blackboard.Dto;

public class BlackboardInitDto:ConfigurationBase
{
    public  string Topic { get; set; }
}