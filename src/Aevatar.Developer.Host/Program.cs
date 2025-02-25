using System;
using System.Threading.Tasks;
using Aevatar.Developer.Host.Extensions;
using Aevatar.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Serilog;
using Serilog.Events;

namespace Aevatar.Developer.Host;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        
        var hostId = configuration["Host:HostId"];
        var version = configuration["Host:Version"];
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("HostId", hostId)
            .Enrich.WithProperty("Version", version)
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting Developer.Host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host
                .UseOrleansClientConfigration()
                .ConfigureDefaults(args)
                .UseAutofac()
                .UseSerilog();
            builder.Services.AddSignalR().AddOrleans();
            await builder.AddApplicationAsync<AevatarDeveloperHostModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            app.MapHub<AevatarSignalRHub>("api/agent/aevatarHub");
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
}