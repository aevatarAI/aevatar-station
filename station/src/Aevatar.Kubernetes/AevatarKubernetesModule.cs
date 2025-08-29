using Aevatar.Options;
using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Aevatar.Kubernetes;

public class AevatarKubernetesModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<KubernetesOptions>(configuration.GetSection("Kubernetes"));
        
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        
        // Only register real Kubernetes client in non-development environments
        if (!hostingEnvironment.IsDevelopment())
        {
            context.Services.AddSingleton<k8s.Kubernetes>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<KubernetesOptions>>().Value;
                if (options == null)
                {
                    throw new Exception("the config of [Kubernetes] is missing.");
                }
                var config = KubernetesClientConfiguration.InClusterConfig();
                return new k8s.Kubernetes(config);
            });
        }
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
        KubernetesConstants.Initialize(configuration);
    }
}