using Aevatar.CQRS.Provider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.Silo;

public class Initialization: IStartupTask
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    
    public Initialization(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }
   
    public Task Execute(CancellationToken cancellationToken)
    {
        var cqrsProvider = _serviceProvider.GetRequiredService<ICQRSProvider>();
        var hostId = _configuration.GetValue<string>("Host:HostId");
        cqrsProvider.SetProjectName(hostId);
        return Task.CompletedTask;
    }
}