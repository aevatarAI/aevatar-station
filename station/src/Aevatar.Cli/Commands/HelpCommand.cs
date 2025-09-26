using System.Reflection;
using System.Text;
using Aevatar.Cli.Args;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Cli.Commands;

public class HelpCommand : IConsoleCommand, ITransientDependency
{
    public const string Name = "help";

    public ILogger<HelpCommand> Logger { get; set; }
    protected AevatarCliOptions AevatarCliOptions { get; }
    protected IServiceScopeFactory ServiceScopeFactory { get; }

    public HelpCommand(IOptions<AevatarCliOptions> cliOptions,
        IServiceScopeFactory serviceScopeFactory)
    {
        ServiceScopeFactory = serviceScopeFactory;
        Logger = NullLogger<HelpCommand>.Instance;
        AevatarCliOptions = cliOptions.Value;
    }

    public Task ExecuteAsync(CommandLineArgs commandLineArgs)
    {
        if (string.IsNullOrWhiteSpace(commandLineArgs.Target))
        {
            Logger.LogInformation(GetUsageInfo());
            return Task.CompletedTask;
        }

        if (!AevatarCliOptions.Commands.TryGetValue(commandLineArgs.Target, out var commandType))
        {
            Logger.LogWarning("There is no command named {Target}.", commandLineArgs.Target);
            Logger.LogInformation(GetUsageInfo());
            return Task.CompletedTask;
        }

        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var command = (IConsoleCommand)scope.ServiceProvider.GetRequiredService(commandType);
            Logger.LogInformation(command.GetUsageInfo());
        }

        return Task.CompletedTask;
    }

    public string GetUsageInfo()
    {
        var sb = new StringBuilder();

        sb.AppendLine("");
        sb.AppendLine("Usage:");
        sb.AppendLine("");
        sb.AppendLine("    aevatar <command> <target> [options]");
        sb.AppendLine("");
        sb.AppendLine("Command List:");
        sb.AppendLine("");

        foreach (var command in AevatarCliOptions.Commands.ToArray().Where(NotHiddenFromCommandList).OrderBy(x => x.Key))
        {
            var method = command.Value.GetMethod("GetShortDescription", BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                continue;
            }

            var shortDescription = (string)method.Invoke(null, null)!;

            sb.Append("    > ");
            sb.Append(command.Key);
            sb.Append(string.IsNullOrWhiteSpace(shortDescription) ? "" : ":");
            sb.Append(' ');
            sb.AppendLine(shortDescription);
        }

        sb.AppendLine("");
        sb.AppendLine("To get a detailed help for a command:");
        sb.AppendLine("");
        sb.AppendLine("    aevatar help <command>");
        sb.AppendLine("");
        // TODO:
        // sb.AppendLine("See the documentation for more info: ");

        return sb.ToString();
    }

    private bool NotHiddenFromCommandList(KeyValuePair<string, Type> command)
    {
        return command.Value.GetCustomAttribute<HideFromCommandList>() == null;
    }
}