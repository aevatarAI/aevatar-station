namespace Aevatar.Core.Abstractions;

/// <summary>
/// Generic factory interface for creating GAgents of type T.
/// T can be either IGAgent or IGAgentPlus or any other grain interface that inherits from IGrainWithGuidKey and implements IActivatable and IConfigurable.
/// </summary>
/// <typeparam name="T">The grain interface type (IGAgent, IGAgentPlus, or derived interfaces)</typeparam>
public interface IGAgentFactory<T> where T : IGAgentBase
{
    Task<T> GetGAgentAsync(GrainId grainId, ConfigurationBase? configuration = null);

    Task<T> GetGAgentAsync(Guid primaryKey, string alias,
        string ns, ConfigurationBase? configuration = null);

    Task<T> GetGAgentAsync(string alias, string ns,
        ConfigurationBase? configuration = null);

    Task<T> GetGAgentAsync(Guid primaryKey, Type gAgentType, ConfigurationBase? configuration = null);

    Task<T> GetGAgentAsync(Type gAgentType, ConfigurationBase? configuration = null);

    Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(Guid primaryKey,
        ConfigurationBase? configuration = null)
        where TGrainInterface : T;
    
    Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(GrainId grainId,
        ConfigurationBase? configuration = null)
        where TGrainInterface : T;

    Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(ConfigurationBase? configuration = null)
        where TGrainInterface : T;
}
