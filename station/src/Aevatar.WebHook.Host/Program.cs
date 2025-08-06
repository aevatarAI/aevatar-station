using System;
using System.Threading.Tasks;
using Aevatar.Webhook.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Aevatar.Domain.Shared.Configuration;

namespace Aevatar.Webhook;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddAevatarSecureConfiguration(
                systemConfigPaths: new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "appsettings.Shared.json")
                })
            .AddEnvironmentVariables()
            .Build();

        var webhookId = configuration["Webhook:WebhookId"];
        var version = configuration["Webhook:Version"];
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting Aevatar.Developer.Host.");
            var builder = CreateHostBuilder(args);
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddAevatarSecureConfiguration(
                    systemConfigPaths: new[]
                    {
                        Path.Combine(AppContext.BaseDirectory, "appsettings.Shared.json")
                    })
                    .AddEnvironmentVariables();
            });
            await builder.Build().RunAsync();
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

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args).UseOrleansClientConfigration()
            .UseAutofac()
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
            .UseSerilog();
    }
}