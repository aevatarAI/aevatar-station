using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Aevatar.Handler;
using Microsoft.AspNetCore.Authorization;
using Orleans.Hosting;
using Serilog;
using Serilog.Events;
using Aevatar.Domain.Shared.Configuration;
using Aevatar.Sandbox.HttpApi.Host.Extensions;

namespace Aevatar.Sandbox;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        ConfigureLogger();

        try
        {
            Log.Information("Starting Sandbox.HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);
            
            builder.Configuration
                .AddAevatarSecureConfiguration(
                    systemConfigPaths: new[]
                    {
                        Path.Combine(AppContext.BaseDirectory, "..", "..", "configurations", "appsettings.Shared.json"),
                        Path.Combine(AppContext.BaseDirectory, "..", "..", "configurations", "appsettings.HttpApi.Host.Shared.json")
                    })
                .AddEnvironmentVariables();

            builder.Host
                .UseOrleansClientConfigration()
                .ConfigureDefaults(args)
                .UseAutofac()
                .UseSerilog();

            builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AevatarAuthorizationMiddlewareResultHandler>();
            
            await builder.AddApplicationAsync<AevatarSandboxHttpApiHostModule>();
            
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

    private static void ConfigureLogger()
    {
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();
    }
}