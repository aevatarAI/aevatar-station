using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.CQRS.Handler;
using Aevatar.CQRS.Provider;
using Aevatar.Service;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Aevatar.GAgent;

public class CqrsServiceTest : AevatarApplicationTestBase
{
    private readonly ICqrsService _cqrsService;
    private readonly IClusterClient _clusterClient;
    private readonly ITestOutputHelper _output;
    private readonly ICQRSProvider _cqrsProvider;
    private readonly Mock<IIndexingService> _mockIndexingService;
    private const string ChainId = "AELF";
    private const string SenderName = "Test";
    private const string Address = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
    private const string IndexName = "aelfagentgstateindex";
    private const string IndexId = "1";
    public CqrsServiceTest(ITestOutputHelper output)
    {
        _output = output;

        _clusterClient = GetRequiredService<IClusterClient>();
        _mockIndexingService = new Mock<IIndexingService>();
        _mockIndexingService.Setup(service => service.SaveOrUpdateStateIndexAsync(It.IsAny<string>(), It.IsAny<StateBase>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton<IIndexingService>(_mockIndexingService.Object); 
        services.AddMediatR(typeof(SaveStateCommandHandler).Assembly);
        services.AddMediatR(typeof(GetStateQueryHandler).Assembly);
        services.AddMediatR(typeof(SendEventCommandHandler).Assembly);
        services.AddMediatR(typeof(SaveGEventCommandHandler).Assembly);
        services.AddMediatR(typeof(GetGEventQueryHandler).Assembly);
        services.AddMediatR(typeof(GetUserInstanceAgentsHandler).Assembly);

        services.AddSingleton<IEventDispatcher,CQRSProvider>();
        services.AddSingleton<ICQRSProvider,CQRSProvider>();
        services.AddSingleton<ICqrsService,CqrsService>();

        services.AddSingleton<IGrainFactory>(_clusterClient);
        var serviceProvider = services.BuildServiceProvider();
        _cqrsProvider = serviceProvider.GetRequiredService<ICQRSProvider>();
        _cqrsService = serviceProvider.GetRequiredService<ICqrsService>();

    }
    
    
    
}