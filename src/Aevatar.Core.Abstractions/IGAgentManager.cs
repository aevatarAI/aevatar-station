namespace Aevatar.Core.Abstractions;

public interface IGAgentManager
{
    List<Type> GetAvailableGAgentTypes();
    List<GrainType> GetAvailableGAgentGrainTypes();
}