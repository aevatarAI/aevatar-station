using Microsoft.Extensions.DependencyInjection;
using Orleans.EventSourcing;

namespace Aevatar.EventSourcing.Core.Storage;

public static class LogConsistencyProviderFactory
{
    public static ILogViewAdaptorFactory Create(IServiceProvider serviceProvider, object? name)
    {
        var logConsistentStorage = serviceProvider.GetRequiredKeyedService<ILogConsistentStorage>(name);
        return ActivatorUtilities.CreateInstance<LogConsistencyProvider>(serviceProvider, logConsistentStorage);
    }
}