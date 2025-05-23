using Aevatar.EventSourcing.Core.Storage;
using Orleans.EventSourcing;
using Orleans.Serialization;
using Orleans.Storage;

namespace Aevatar.EventSourcing.Core.Storage;

public class LogConsistencyProvider : ILogViewAdaptorFactory
{
    private readonly ILogConsistentStorage _logConsistentStorage;
    private readonly DeepCopier _deepCopier;
    private readonly IServiceProvider _serviceProvider;

    public bool UsesStorageProvider => true;

    public LogConsistencyProvider(ILogConsistentStorage logConsistentStorage, DeepCopier deepCopier, IServiceProvider serviceProvider)
    {
        _logConsistentStorage = logConsistentStorage;
        _deepCopier = deepCopier;
        _serviceProvider = serviceProvider;
    }

    public ILogViewAdaptor<TLogView, TLogEntry> MakeLogViewAdaptor<TLogView, TLogEntry>(
        ILogViewAdaptorHost<TLogView, TLogEntry> hostGrain, TLogView initialState,
        string grainTypeName, IGrainStorage grainStorage, ILogConsistencyProtocolServices services)
        where TLogView : class, new()
        where TLogEntry : class
    {
        return new LogViewAdaptor<TLogView, TLogEntry>(hostGrain, initialState, grainStorage, grainTypeName, services,
            _logConsistentStorage, _deepCopier);
    }
}