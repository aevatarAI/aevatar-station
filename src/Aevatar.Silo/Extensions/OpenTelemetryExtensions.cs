using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Aevatar.Silo.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddAevatarOpenTelemetry(this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "Aevatar.Silo";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0";

        return services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(serviceName)
                    .AddSource("Aevatar.Messaging") // Make sure this matches the source name exactly
                    .AddSource("Orleans.Runtime")
                    .AddSource("Orleans.Messaging")
                    .AddSource("Microsoft.Orleans")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            })
            .WithMetrics(metrics => metrics
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddMeter("Microsoft.Orleans")
                .AddMeter("Aevatar.Messaging")
                .AddMeter(serviceName)
            )
            .Services;
    }
}