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
using Microsoft.AspNetCore.Rewrite;
using Orleans.Hosting;
using Serilog;
using Serilog.Events;
using Aevatar.Domain.Shared.Configuration;
using Aevatar.Core.Interception.Extensions;

namespace Aevatar;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        try
        {
            Log.Information("Starting HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);
            
            // Configure all configuration sources once
            ConfigureAppConfiguration(builder.Configuration, args);
            ConfigureLogger(builder.Configuration);
            
            builder.Host
                .UseOrleansClientConfiguration()
                .UseAutofac()
                .UseSerilog();
            builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; }).AddOrleans();
            builder.Services
                .AddSingleton<IAuthorizationMiddlewareResultHandler, AevatarAuthorizationMiddlewareResultHandler>();
            await builder.AddApplicationAsync<AevatarHttpApiHostModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            
            // Add trace context middleware to capture trace IDs from HTTP requests
            app.UseTraceContext();
            
            // URL rewriting for local development only
            if (app.Environment.IsDevelopment())
            {
                var rewriteOptions = new RewriteOptions()
                    .AddRewrite(@"^/[^/]*-client(/.*)?$", "$1", skipRemainingRules: true);
                app.UseRewriter(rewriteOptions);
            }
            
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

    private static void ConfigureAppConfiguration(IConfigurationBuilder configBuilder, string[] args)
    {
        // Clear default configuration sources to avoid duplicate loading
        configBuilder.Sources.Clear();
        configBuilder
            .AddAevatarSecureConfiguration(
                systemConfigPaths: new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "appsettings.Shared.json"),
                    Path.Combine(AppContext.BaseDirectory, "appsettings.HttpApi.Host.Shared.json")
                })
            .AddEnvironmentVariables()
            .AddCommandLine(args);
            
        Log.Information("Configuration loaded with ephemeral config support");
    }
    
    private static void ConfigureLogger(IConfiguration configuration, LoggerConfiguration? loggerConfiguration = null)
    {
        Log.Logger = (loggerConfiguration ?? new LoggerConfiguration())
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .CreateLogger();
            
        var corsOrigins = configuration["App:CorsOrigins"];
        Log.Information("Application configured with CORS origins: {CorsOrigins}", corsOrigins);
    }
}