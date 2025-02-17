using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Aevatar.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace Aevatar;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        ConfigureLogger();

        try
        {
            Log.Information("Starting HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host
                .UseOrleansClientConfiguration()
                .ConfigureDefaults(args)
                .UseAutofac()
                .UseSerilog();
            await builder.AddApplicationAsync<AevatarHttpApiHostModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
    
    private static void ConfigureLogger(LoggerConfiguration? loggerConfiguration = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        Log.Logger = (loggerConfiguration ?? new LoggerConfiguration())
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.Console())
            .CreateLogger();
    }
}
