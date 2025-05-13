using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Silo.Extensions;
using Aevatar.Silo.Observability;
using Serilog;

namespace Aevatar.Silo;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
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
                // Configure OpenTelemetry
                services.AddAevatarOpenTelemetry(hostContext.Configuration);
                services.AddApplication<SiloModule>();
            })
            .UseOrleansConfiguration()
            .UseAutofac()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                services.UseGrainStorageWithMetrics();
            });
}