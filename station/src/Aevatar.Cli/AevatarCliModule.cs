using System.Text;
using Aevatar.Cli.Commands;
using Aevatar.Cli.Http;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.Http;
using Volo.Abp.Json;
using Volo.Abp.Modularity;

namespace Aevatar.Cli;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpHttpModule),
    typeof(AbpJsonModule)
)]
public class AevatarCliModule : AbpModule
{
public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClient(AevatarCliConstants.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new CliHttpClientHandler());

        context.Services.AddHttpClient(AevatarCliConstants.GithubHttpClientName, client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyAgent/1.0");
        });

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Configure<AevatarCliOptions>(options =>
        {
            options.Commands[HelpCommand.Name] = typeof(HelpCommand);
            options.Commands[AgentCommand.Name] = typeof(AgentCommand);
            options.Commands[WorkflowCommand.Name] = typeof(WorkflowCommand);
            options.Commands[UtilCommand.Name] = typeof(UtilCommand);
            options.Commands[ConfigCommand.Name] = typeof(ConfigCommand);
        });
    }
}
