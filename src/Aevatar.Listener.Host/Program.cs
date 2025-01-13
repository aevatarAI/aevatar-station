using System;
using System.Threading.Tasks;
using Aevatar.Listener.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Aevatar.Listener;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        
        var ListenerId = configuration["ListenerInfo:ListenerId"];
        var version = configuration["ListenerInfo:Version"];
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ListenerId", ListenerId)
            .Enrich.WithProperty("Version", version)
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting Aevatar.Listener.Host.");
            var builder = WebApplication.CreateBuilder(args);
            OrleansHostExtensions.UseOrleansClient(builder.Host)
                .UseAutofac()
                .UseSerilog();
            await builder.AddApplicationAsync<AevatarListenerHostModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
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
}