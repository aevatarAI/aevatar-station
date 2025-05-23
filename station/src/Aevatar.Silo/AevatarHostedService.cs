using Microsoft.Extensions.Hosting;
using Volo.Abp;

namespace Aevatar.Silo;

public class AevatarHostedService : IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;

    public AevatarHostedService(
        IAbpApplicationWithExternalServiceProvider application,
        IServiceProvider serviceProvider)
    {
        _application = application;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _application.Initialize(_serviceProvider);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application.Shutdown();
        return Task.CompletedTask;
    }
}