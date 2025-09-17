using Aevatar.GAgents.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Device.Http;

/// <summary>
/// Aevatar Virtual Device GAgent Module
/// </summary>
[DependsOn(
    typeof(AevatarGAgentsDeviceModule),
    typeof(AevatarGAgentsCommonModule)
)]
public class AevatarGAgentsHttpDeviceModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        
        // Register HTTP client for Virtual Device Hub API
        services.AddHttpClient<HttpDeviceConnection>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        // Register Virtual Device API connection factory
        services.AddTransient<IVirtualDeviceApiConnectionFactory, VirtualDeviceApiConnectionFactory>();
        
        // Configure Virtual Device Hub options
        services.Configure<HttpDeviceHubOptions>(options =>
        {
            options.BaseUrl = "https://localhost:9001"; // Default to HTTPS development server
            options.ApiVersion = "v1";
            options.TimeoutSeconds = 30;
            options.RetryAttempts = 3;
            options.RetryDelaySeconds = 2;
        });
    }
}

/// <summary>
/// Virtual Device API Connection Factory interface
/// </summary>
public interface IVirtualDeviceApiConnectionFactory
{
    /// <summary>
    /// Create virtual device API connection
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <param name="deviceType">Device type</param>
    /// <param name="options">Connection options</param>
    /// <returns>API connection instance</returns>
    HttpDeviceConnection CreateConnection(string deviceId, string deviceType, HttpDeviceHubOptions? options = null);
}

/// <summary>
/// Virtual Device API Connection Factory implementation
/// </summary>
public class VirtualDeviceApiConnectionFactory : IVirtualDeviceApiConnectionFactory
{
    private readonly IServiceProvider _serviceProvider;

    public VirtualDeviceApiConnectionFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public HttpDeviceConnection CreateConnection(string deviceId, string deviceType, HttpDeviceHubOptions? options = null)
    {
        var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        var logger = _serviceProvider.GetRequiredService<ILogger<HttpDeviceConnection>>();
        
        return new HttpDeviceConnection(httpClient, logger, deviceId, deviceType, options);
    }
}
