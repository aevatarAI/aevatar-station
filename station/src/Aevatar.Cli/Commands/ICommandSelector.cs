using Aevatar.Cli.Args;

namespace Aevatar.Cli.Commands;

public interface ICommandSelector
{
    Type Select(CommandLineArgs commandLineArgs);
}