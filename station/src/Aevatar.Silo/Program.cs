using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Silo.Extensions;
using Aevatar.Silo.Observability;
using Aevatar.Options;
using Aevatar.Domain.Shared.Configuration;
using Serilog;
using Microsoft.AspNetCore.Hosting;
namespace Aevatar.Silo;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        // Register the label provider before building the silo host
        HistogramAggregatorExtension.SetLabelProvider(new AevatarMetricLabelProvider());
        var configuration = new ConfigurationBuilder()
            .AddAevatarSecureConfiguration(
                systemConfigPaths: new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "appsettings.Shared.json"),
                    Path.Combine(AppContext.BaseDirectory, "appsettings.Silo.Shared.json")
                })
            .AddEnvironmentVariables()
            .Build();
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting Silo");
            var builder = CreateHostBuilder(args);
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddAevatarSecureConfiguration(
                        systemConfigPaths: new[]
                        {
                            Path.Combine(AppContext.BaseDirectory, "appsettings.Shared.json"),
                            Path.Combine(AppContext.BaseDirectory, "appsettings.Silo.Shared.json")
                        })
                    .AddEnvironmentVariables();
            });
            var app = builder.Build();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    internal static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                // Configure the health check port from configuration
                webBuilder.ConfigureKestrel((context, options) =>
                {
                    var healthCheckOptions = context.Configuration.GetSection("HealthCheck").Get<HealthCheckOptions>() ?? new HealthCheckOptions();
                    options.ListenAnyIP(healthCheckOptions.Port);
                });
                
                webBuilder.Configure(app =>
                {
                    app.MapOrleansHealthChecks();
                });
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddApplication<SiloModule>();
                // Health checks are added in SiloModule
            })
            .UseOrleansConfiguration()
            .UseServiceProviderFactory(new DiagnosticAutofacServiceProviderFactory())
            .UseSerilog()
            ;

    // Pluggable metric label provider for Orleans metrics
    public class AevatarMetricLabelProvider : IMetricLabelProvider
    {
        public IEnumerable<KeyValuePair<string, object>> GetLabels(object context = null)
        {
            if (context != null)
            {
                var msgProp = context.GetType().GetProperty("Message");
                var reqProp = context.GetType().GetProperty("Request");
                var msg = msgProp?.GetValue(context, null);
                var req = reqProp?.GetValue(context, null);
                var grainClassTypeProp = msg?.GetType().GetProperty("GrainClassType");
                var grainClassType = grainClassTypeProp?.GetValue(msg, null) as Type;
                var grainType = grainClassType != null ? grainClassType.Name : msg?.GetType().GetProperty("InterfaceType")?.GetValue(msg, null)?.ToString() ?? "unknown";
                var methodName = req?.GetType().GetMethod("GetMethodName")?.Invoke(req, null)?.ToString() ?? "unknown";
                yield return new KeyValuePair<string, object>("grain_type", grainType);
                yield return new KeyValuePair<string, object>("method_name", methodName);
            }
        }
    }
}