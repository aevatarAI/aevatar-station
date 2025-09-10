using System;
using System.IO;
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
using Aevatar.Domain.Shared.Configuration;

namespace Aevatar.Developer.Host;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        try
        {
            Log.Information("Starting Developer.Host.");
            var builder = WebApplication.CreateBuilder(args);
            
            // Configure all configuration sources once
            ConfigureAppConfiguration(builder.Configuration, args);
            ConfigureLogger(builder.Configuration);
            
            builder.Host
                .UseOrleansClientConfigration()
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
    
    private static void ConfigureAppConfiguration(IConfigurationBuilder configBuilder, string[] args)
    {
        configBuilder
            .AddAevatarSecureConfiguration(
                systemConfigPaths: new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "appsettings.Shared.json"),
                    Path.Combine(AppContext.BaseDirectory, "appsettings.HttpApi.Host.Shared.json")
                })
            .AddEnvironmentVariables()
            .AddCommandLine(args);
            
        Log.Information("Developer.Host configuration loaded with ephemeral config support");
    }
    
    private static void ConfigureLogger(IConfiguration configuration, LoggerConfiguration? loggerConfiguration = null)
    {
        var hostId = configuration["Host:HostId"];
        var version = configuration["Host:Version"];
        
        Log.Logger = (loggerConfiguration ?? new LoggerConfiguration())
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
            
        var corsOrigins = configuration["App:CorsOrigins"];
        Log.Information("Developer.Host configured with CORS origins: {CorsOrigins}", corsOrigins);
    }
}