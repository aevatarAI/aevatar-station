<<<<<<< HEAD
﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PluginGAgent.Silo.Extensions;
using Serilog;

namespace PluginGAgent.Silo;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
=======
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Silo.Extensions;
using Aevatar.Silo.Observability;
using Serilog;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using System.Linq;
using Orleans.Runtime;

namespace Aevatar.Silo;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        // Register the label provider before building the silo host
        HistogramAggregatorExtension.SetLabelProvider(new AevatarMetricLabelProvider());
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Shared.json"))
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Silo.Shared.json"))
>>>>>>> origin/dev
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.secrets.json", optional: true)
            .Build();
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting Silo");
<<<<<<< HEAD
            await CreateHostBuilder(args).RunConsoleAsync();
=======
            var builder = CreateHostBuilder(args);
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Shared.json"))
                    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Silo.Shared.json"))
                    .AddJsonFile("appsettings.json");
            });
            var app = builder.Build();
            await app.RunAsync();
>>>>>>> origin/dev
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
<<<<<<< HEAD
            .ConfigureServices((_, services) => { services.AddApplication<PluginGAgentTestModule>(); })
            .UseOrleansConfiguration()
            .UseAutofac()
            .UseSerilog();
=======
            .ConfigureServices((hostContext, services) =>
            {
                services.AddApplication<SiloModule>();
            })
            .UseOrleansConfiguration()
            .UseServiceProviderFactory(new DiagnosticAutofacServiceProviderFactory())
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Configure OpenTelemetry
                services.AddAevatarOpenTelemetry(context.Configuration);
                services.UseGrainStorageWithMetrics();
            });

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
>>>>>>> origin/dev
}