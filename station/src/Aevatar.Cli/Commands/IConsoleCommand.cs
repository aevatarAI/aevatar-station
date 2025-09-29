using Aevatar.Cli.Args;

namespace Aevatar.Cli.Commands;

public interface IConsoleCommand
{
    Task ExecuteAsync(CommandLineArgs commandLineArgs);

    string GetUsageInfo();
}