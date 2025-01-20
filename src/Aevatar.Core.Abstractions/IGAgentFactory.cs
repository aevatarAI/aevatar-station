namespace Aevatar.Core.Abstractions;

public interface IGAgentFactory
{
    Task<IGAgent> GetGAgentAsync(GrainId grainId, InitializationEventBase? initializationEvent = null);

    Task<IGAgent> GetGAgentAsync(string alias, Guid primaryKey,
        string ns = AevatarCoreConstants.GAgentDefaultNamespace, InitializationEventBase? initializationEvent = null);

    Task<IGAgent> GetGAgentAsync(string alias, string ns = AevatarCoreConstants.GAgentDefaultNamespace,
        InitializationEventBase? initializationEvent = null);
    
    Task<IGAgent> GetGAgentAsync(Type gAgentType, Guid primaryKey, InitializationEventBase? initializationEvent = null);

    Task<IGAgent> GetGAgentAsync(Type gAgentType, InitializationEventBase? initializationEvent = null);

    Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(Guid primaryKey, InitializationEventBase? initializationEvent = null)
        where TGrainInterface : IGAgent;

    Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(InitializationEventBase? initializationEvent = null)
        where TGrainInterface : IGAgent;
}