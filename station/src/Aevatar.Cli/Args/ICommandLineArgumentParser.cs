namespace Aevatar.Cli.Args;

public interface ICommandLineArgumentParser
{
    CommandLineArgs Parse(string[] args);

    CommandLineArgs Parse(string lineText);
}
