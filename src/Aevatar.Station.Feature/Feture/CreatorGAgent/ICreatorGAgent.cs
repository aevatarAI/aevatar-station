using Aevatar.Core.Abstractions;

namespace Aevatar.Station.Feature.CreatorGAgent;

public interface ICreatorGAgent : IStateGAgent<CreatorGAgentState>
{
    Task<CreatorGAgentState> GetAgentAsync();
    Task CreateAgentAsync(AgentData agentData);
    Task UpdateAgentAsync(UpdateAgentInput dto);
    Task DeleteAgentAsync();
    Task PublishEventAsync<T>(T @event) where T : EventBase;
    Task UpdateAvailableEventsAsync(List<Type>? eventTypeList);
}