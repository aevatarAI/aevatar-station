using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar;

public sealed class LoggerInjectionGrainActivator(IServiceProvider serviceProvider) : IGrainActivator
{
    public object CreateInstance(IGrainContext context)
    {
        var grain = ActivatorUtilities.CreateInstance(serviceProvider, context.GrainInstance!.GetType());
        InjectLogger(grain);
        return grain;
    }

    private void InjectLogger(object grain)
    {
        var grainType = grain.GetType();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(grainType);

        var loggerProperty = grainType.GetProperty("Logger");
        if (loggerProperty != null && loggerProperty.CanWrite)
        {
            loggerProperty.SetValue(grain, logger);
        }
    }

    public ValueTask DisposeInstance(IGrainContext context, object instance)
    {
        return ValueTask.CompletedTask;
    }
}