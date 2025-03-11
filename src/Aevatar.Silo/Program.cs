using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Silo.Extensions;
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
            
            ThreadPool.GetMinThreads(out int minWorker, out int minIo);
            ThreadPool.GetAvailableThreads(out int availWorker, out int availIo);
            Log.Information($"Silo MinThreads: Worker={minWorker}, IO={minIo}");
            Log.Information($"Silo AvailableThreads: Worker={availWorker}, IO={availIo}");
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
            .ConfigureServices((hostcontext, services) =>
            {
                services.AddApplication<SiloModule>();
            })
            .UseOrleansConfiguration()
            .UseAutofac()
            .UseSerilog();
}