using Aevatar.Cli.Args;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Cli.Commands;

public class CommandSelector : ICommandSelector, ITransientDependency
{
    protected AevatarCliOptions Options { get; }

    public CommandSelector(IOptions<AevatarCliOptions> options)
    {
        Options = options.Value;
    }

    public Type Select(CommandLineArgs commandLineArgs)
    {
        if (commandLineArgs.Command.IsNullOrWhiteSpace())
        {
            return typeof(HelpCommand);
        }

        return Options.Commands.GetOrDefault(commandLineArgs.Command)
               ?? typeof(HelpCommand);
    }
}