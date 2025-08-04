using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Aevatar.Extensions;
using Aevatar.Handler;
using Aevatar.Hubs;
using Aevatar.SignalR;
using Microsoft.AspNetCore.Authorization;
using Orleans.Hosting;
using Serilog;
using Serilog.Events;
using Aevatar.Domain.Shared.Configuration;

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
            builder.Configuration
                .AddAevatarSecureConfiguration(
                    systemConfigPaths: new[]
                    {
                        Path.Combine(AppContext.BaseDirectory, "appsettings.Shared.json"),
                        Path.Combine(AppContext.BaseDirectory, "appsettings.HttpApi.Host.Shared.json")
                    })
                .AddEnvironmentVariables();
            builder.Host
                .UseOrleansClientConfiguration()
                .ConfigureDefaults(args)
                .UseAutofac()
                .UseSerilog();
            builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; }).AddOrleans();
            builder.Services
                .AddSingleton<IAuthorizationMiddlewareResultHandler, AevatarAuthorizationMiddlewareResultHandler>();
            await builder.AddApplicationAsync<AevatarHttpApiHostModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            app.MapHub<AevatarSignalRHub>("api/agent/aevatarHub");
            app.MapHub<StationSignalRHub>("api/notifications").RequireAuthorization();

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
            .AddAevatarSecureConfiguration(
                systemConfigPaths: new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "appsettings.Shared.json"),
                    Path.Combine(AppContext.BaseDirectory, "appsettings.HttpApi.Host.Shared.json")
                })
            .AddEnvironmentVariables()
            .Build();
        Log.Logger = (loggerConfiguration ?? new LoggerConfiguration())
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .CreateLogger();
    }
}