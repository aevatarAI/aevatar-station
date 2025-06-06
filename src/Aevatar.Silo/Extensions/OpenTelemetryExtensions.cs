using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orleans.Configuration;

namespace Aevatar.Silo.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddAevatarOpenTelemetry(this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "Aevatar.Silo";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0";
        var endpoint = configuration["OpenTelemetry:CollectorEndpoint"] ?? "http://localhost:4315";

        var serviceProvider = services.BuildServiceProvider();
        var siloOptions = serviceProvider.GetService<IOptions<SiloOptions>>();
        return services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceInstanceId: $"{Guid.NewGuid()}_{siloOptions!.Value.SiloName}",
                serviceVersion: serviceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(serviceName)
                    .AddSource("Aevatar.Messaging")
                    .AddSource("Orleans.Runtime")
                    .AddSource("Orleans.Messaging")
                    .AddSource("Microsoft.Orleans")
                    .AddSource("Aevatar.CQRS")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            })
            .WithMetrics(metrics => metrics
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddMeter("Microsoft.Orleans")
                .AddMeter("Aevatar.Messaging")
                .AddMeter("Aevatar.CQRS")
                .AddMeter("Aevatar.Storage")
                .AddMeter(serviceName)
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(endpoint);
                })
            )
            .Services;
    }
}