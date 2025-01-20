namespace Aevatar.Core.Abstractions;

public interface IGAgentFactory
{
    Task<IGAgent> GetGAgentAsync(GrainId grainId, InitializationEventBase? initializeDto = null);

    Task<IGAgent> GetGAgentAsync(string alias, Guid primaryKey,
        string ns = AevatarCoreConstants.GAgentDefaultNamespace, InitializationEventBase? initializeDto = null);

    Task<IGAgent> GetGAgentAsync(string alias, string ns = AevatarCoreConstants.GAgentDefaultNamespace,
        InitializationEventBase? initializeDto = null);
    
    Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(Guid primaryKey, InitializationEventBase? initializeDto = null)
        where TGrainInterface : IGAgent;

    Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(InitializationEventBase? initializeDto = null)
        where TGrainInterface : IGAgent;
}