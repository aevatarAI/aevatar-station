using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orleans.Configuration;
using OpenTelemetry;
using System.Diagnostics.Metrics;
using Aevatar.Core;
using Aevatar.Core.Observability;

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
        var clusterId = configuration["Orleans:ClusterId"] ?? "default-cluster";
        var siloId = siloOptions?.Value.SiloName ?? "default-silo";
        return services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceInstanceId: $"{Guid.NewGuid()}_{siloId}",
                    serviceVersion: serviceVersion)
                .AddAttributes(new []
                {
                    new KeyValuePair<string, object>("cluster_id", clusterId),
                    new KeyValuePair<string, object>("silo_id", siloId)
                })
            )
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(serviceName)
                    .AddSource("Aevatar.Messaging")
                    .AddSource("Orleans.Runtime")
                    .AddSource("Orleans.Messaging")
                    .AddSource("Microsoft.Orleans")
                    .AddSource("Aevatar.CQRS")
                    .AddSource("LatencyBenchmark.Root")
                    .AddSource("LatencyPublisherAgent")
                    .AddSource("LatencyBenchmark.Handler")
                    .AddSource("Aevatar.Core.GAgent")
                    .AddSource("Aevatar.Core.Observer")
                    .AddSource("Orleans.Streaming.PullingAgent")
                    .AddSource("Orleans.Streaming.Kafka")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            })
            .WithMetrics(metrics => metrics
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()           // .NET Runtime metrics (GC, ThreadPool, Memory, Exceptions)
                .AddMeter("System.Runtime")            // Additional .NET 9+ runtime metrics
                .AddMeter("Microsoft.Orleans")         // Orleans built-in metrics
                .AddMeter("Aevatar.Messaging")
                .AddMeter("Aevatar.CQRS")
                .AddMeter("Aevatar.Storage")
                .AddMeter(serviceName)
                .AddMeter(OpenTelemetryConstants.AevatarStreamsMeterName)
                .AddView(OpenTelemetryConstants.EventPublishLatencyHistogram, new ExplicitBucketHistogramConfiguration 
                { 
                    Boundaries = new double[] { 0.1, 0.2, 0.5, 0.75, 1.0, 1.5, 2.0, 5.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0, 100.0 }  // Boundaries in seconds
                })
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(endpoint);
                })
            )
            .Services;
    }
}