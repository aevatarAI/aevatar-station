using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Silo.Extensions;
using Aevatar.Silo.Observability;
using Serilog;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using System.Linq;

namespace Aevatar.Silo;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Shared.json"))
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Silo.Shared.json"))
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
            var builder = CreateHostBuilder(args);
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Shared.json"))
                    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Silo.Shared.json"))
                    .AddJsonFile("appsettings.json");
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
}