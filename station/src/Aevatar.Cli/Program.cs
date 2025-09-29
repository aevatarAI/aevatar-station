using Aevatar.Cli;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Volo.Abp;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var loggerOutputTemplate = "{Message:lj}{NewLine}{Exception}";
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Volo.Abp", LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
    .MinimumLevel.Override("Volo.Abp.IdentityModel", LogEventLevel.Information)
#if DEBUG
    .MinimumLevel.Override("Aevatar.Cli", LogEventLevel.Debug)
#else
            .MinimumLevel.Override("Aevatar.Cli", LogEventLevel.Information)
#endif
    .Enrich.FromLogContext()
    .WriteTo.File(Path.Combine(AevatarCliConstants.Paths.Log, "aevatar-cli-logs.txt"), outputTemplate: loggerOutputTemplate)
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen, outputTemplate: loggerOutputTemplate)
    .CreateLogger();

using (var application = AbpApplicationFactory.Create<AevatarCliModule>(
           options =>
           {
               options.UseAutofac();
               options.Services.AddLogging(c => c.AddSerilog());
           }))
{
    application.Initialize();

    await application.ServiceProvider
        .GetRequiredService<AevatarCliService>()
        .RunAsync(args);

    application.Shutdown();

    Log.CloseAndFlush();
}