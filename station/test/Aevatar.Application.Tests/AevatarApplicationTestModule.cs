using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.CQRS.Handler;
using Aevatar.Kubernetes.Manager;
using Aevatar.Kubernetes.Adapter;
using Aevatar.Kubernetes.ResourceDefinition;
using Aevatar.Options;
using Aevatar.SignalR;
using Aevatar.Mock;
using Aevatar.SignalR;
using Aevatar.SignalR.SignalRMessage;
using Aevatar.WebHook.Deploy;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Ingest;
using Elastic.Transport;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver.Core.Configuration;
using Moq;
using Volo.Abp.AutoMapper;
using Volo.Abp.Emailing;
using Volo.Abp.EventBus;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using ChatConfigOptions = Aevatar.Options.ChatConfigOptions;
using Moq;
using k8s.Models;
using k8s;

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
        
        // 添加Mock的IIdentityRoleRepository来解决依赖注入问题
        context.Services.AddTransient<IIdentityRoleRepository>(provider =>
        {
            var mockRepo = new Mock<IIdentityRoleRepository>();
            return mockRepo.Object;
        });
        
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
        
        context.Services.AddTransient<IHubService>(o=>Moq.Mock.Of<IHubService>());
        
        // 添加Mock的Kubernetes客户端适配器以避免真实的kubeconfig文件依赖
        context.Services.AddSingleton<IKubernetesClientAdapter>(provider => CreateMockKubernetesClientAdapter());
        
        // 添加Mock的k8s.Kubernetes客户端以避免真实的kubeconfig文件依赖
        context.Services.AddSingleton<k8s.Kubernetes>(provider => CreateMockKubernetesClient());

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
    
    /// <summary>
    /// 创建Mock的Kubernetes客户端适配器，用于测试环境避免真实的kubeconfig文件依赖
    /// </summary>
    private static IKubernetesClientAdapter CreateMockKubernetesClientAdapter()
    {
        var mock = new Mock<IKubernetesClientAdapter>();
        
        // 创建一个追踪deployment的集合
        var deployments = new List<V1Deployment>();
        
        // 为所有方法提供默认的Mock实现
        mock.Setup(x => x.ListNamespaceAsync()).ReturnsAsync(new V1NamespaceList());
        mock.Setup(x => x.ReadNamespaceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1Namespace());
        mock.Setup(x => x.ListPodsInNamespaceWithPagingAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((new V1PodList(), ""));
        mock.Setup(x => x.ListPodsInNamespaceWithPagingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1PodList());
        mock.Setup(x => x.ListConfigMapAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1ConfigMapList());
        
        // 对ListDeploymentAsync进行特殊处理，返回追踪的deployment列表
        mock.Setup(x => x.ListDeploymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new V1DeploymentList { Items = deployments });
            
        mock.Setup(x => x.ListServiceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1ServiceList());
        mock.Setup(x => x.ListIngressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1IngressList());
        mock.Setup(x => x.CreateNamespaceAsync(It.IsAny<V1Namespace>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1Namespace());
        mock.Setup(x => x.CreateConfigMapAsync(It.IsAny<V1ConfigMap>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1ConfigMap());
        
        // 对CreateDeploymentAsync进行特殊处理，添加deployment到追踪列表
        mock.Setup(x => x.CreateDeploymentAsync(It.IsAny<V1Deployment>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<V1Deployment, string, CancellationToken>((deployment, ns, ct) =>
            {
                // 添加deployment到追踪列表
                deployments.Add(deployment);
            })
            .ReturnsAsync((V1Deployment deployment, string ns, CancellationToken ct) => deployment);
            
        mock.Setup(x => x.CreateServiceAsync(It.IsAny<V1Service>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1Service());
        mock.Setup(x => x.CreateIngressAsync(It.IsAny<V1Ingress>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1Ingress());
        mock.Setup(x => x.DeleteConfigMapAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1Status());
        
        // 对DeleteDeploymentAsync进行特殊处理，从追踪列表中删除deployment
        mock.Setup(x => x.DeleteDeploymentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((name, ns, ct) =>
            {
                // 从追踪列表中删除指定的deployment
                deployments.RemoveAll(d => d.Metadata?.Name == name);
            })
            .ReturnsAsync(new V1Status());
            
        mock.Setup(x => x.DeleteServiceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1Service());
        mock.Setup(x => x.DeleteIngressAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1Status());
        mock.Setup(x => x.ReadNamespacedDeploymentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1Deployment());
        mock.Setup(x => x.ReplaceNamespacedDeploymentAsync(It.IsAny<V1Deployment>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1Deployment());
        mock.Setup(x => x.ReadNamespacedConfigMapAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1ConfigMap());
        mock.Setup(x => x.ReplaceNamespacedConfigMapAsync(It.IsAny<V1ConfigMap>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V1ConfigMap());
        mock.Setup(x => x.ReadNamespacedHorizontalPodAutoscalerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V2HorizontalPodAutoscaler());
        mock.Setup(x => x.CreateNamespacedHorizontalPodAutoscalerAsync(It.IsAny<V2HorizontalPodAutoscaler>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new V2HorizontalPodAutoscaler());
        mock.Setup(x => x.NamespaceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        
        return mock.Object;
    }
    
    /// <summary>
    /// 创建Mock的k8s.Kubernetes客户端，用于测试环境避免真实的kubeconfig文件依赖
    /// </summary>
    private static k8s.Kubernetes CreateMockKubernetesClient()
    {
        var mock = new Mock<k8s.Kubernetes>();
        return mock.Object;
    }
}