using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.SignalR;
using Aevatar.SignalR.SignalRMessage;
using Aevatar.WebHook.Deploy;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Ingest;
using Elastic.Transport;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.AutoMapper;
using Volo.Abp.Emailing;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using ChatConfigOptions = Aevatar.Options.ChatConfigOptions;

namespace Aevatar;

[DependsOn(
    typeof(AevatarApplicationModule),
    typeof(AbpEventBusModule),
    typeof(AevatarOrleansTestBaseModule),
    typeof(AevatarDomainTestModule)
)]
public class AevatarApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarApplicationModule>(); });
        var configuration = context.Services.GetConfiguration();
        Configure<ChatConfigOptions>(configuration.GetSection("Chat"));
        context.Services.AddSingleton<ElasticsearchClient>(sp =>
        {
            var response = TestableResponseFactory.CreateSuccessfulResponse<SearchResponse<Document>>(new(), 200);
            var mock = new Mock<ElasticsearchClient>();
            mock
                .Setup(m => m.SearchAsync<Document>(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            return mock.Object;
        });

        context.Services.AddTransient<IHostDeployManager, DefaultHostDeployManager>();

        context.Services.AddTransient<IHostDeployManager, DefaultHostDeployManager>();

        context.Services.AddSingleton<IEmailSender, NullEmailSender>();

        context.Services.AddTransient<IHubService>(o => Moq.Mock.Of<IHubService>());

        AddMock(context.Services);
    }

    private void AddMock(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IHubService>(provider =>
        {
            var mockHubService = new Mock<IHubService>();

            mockHubService.Setup(f => f.ResponseAsync(It.IsAny<List<Guid>>(), It.IsAny<NotificationResponse>()))
                .Returns(Task.CompletedTask);
            return mockHubService.Object;
        });
    }
}