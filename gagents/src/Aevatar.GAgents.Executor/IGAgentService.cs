namespace Aevatar.GAgents.Executor;

public interface IGAgentService
{
    Task<Dictionary<GrainType, List<Type>>> GetAllAvailableGAgentInformation();
    Task<GAgentDetailInfo> GetGAgentDetailInfoAsync(GrainType grainType);
    Task<List<GrainType>> FindGAgentsByEventTypeAsync(Type eventType);
}