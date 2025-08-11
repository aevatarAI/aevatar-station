using Serilog;
using Serilog.Events;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Aevatar.Domain.Shared.Configuration;
namespace Aevatar.AuthServer;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddAevatarSecureConfiguration()
            .AddEnvironmentVariables()
            .Build();
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Debug()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            // .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
            // .WriteTo.Async(c => c.File("Logs/logs.txt"))
#if DEBUG
            .WriteTo.Async(c => c.Console())
#endif
            .CreateLogger();

        try
        {
            Log.Information("Starting AevatarAuthServer.AuthServer.");
            var builder = WebApplication.CreateBuilder(args);
            // builder.Configuration.AddJsonFile("apollo.appsettings.json");
            builder.Host.AddAppSettingsSecretsJson()
                .UseAutofac()
                // .UseApollo()
                .UseSerilog()
                // .UseOrleansClient()
                ;
            await builder.AddApplicationAsync<AevatarAuthServerModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            //CreateHostBuilder(args).Build().Run();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Aevatar AuthServer terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseOrleansClient()
            .UseAutofac()
            //.ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
            .UseSerilog();
    }
}
