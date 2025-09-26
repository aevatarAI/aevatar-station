using System.Reflection;
using Aevatar.Cli.Helpers;
using NuGet.Versioning;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Cli.Version;

public class CliVersionService : ITransientDependency
{
    private CmdHelper CmdHelper { get; }

    public CliVersionService(CmdHelper cmdHelper)
    {
        CmdHelper = cmdHelper;
    }

    public async Task<SemanticVersion> GetCurrentCliVersionAsync()
    {
        SemanticVersion currentCliVersion = default;

        var consoleOutput = new StringReader(CmdHelper.RunCmdAndGetOutput($"dotnet tool list -g", out int _));
        while (await consoleOutput.ReadLineAsync() is { } line)
        {
            if (line.StartsWith("aevatar.cli", StringComparison.InvariantCultureIgnoreCase))
            {
                var version = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries)[1];

                SemanticVersion.TryParse(version, out currentCliVersion);

                break;
            }
            // if (line.StartsWith("aevatar.station.cli", StringComparison.InvariantCultureIgnoreCase))
            // {
            //     var assemblyVersion = string.Join(".", Assembly.GetExecutingAssembly().GetFileVersion().Split('.').Take(3));
            //     return SemanticVersion.Parse(assemblyVersion + "-station");
            // }
        }

        if (currentCliVersion == null)
        {
            // If not a tool executable, fallback to assembly version and treat as dev without updates
            // Assembly revisions are not supported by SemVer scheme required for NuGet, trim to {major}.{minor}.{patch}
            var assemblyVersion = string.Join(".", Assembly.GetExecutingAssembly().GetFileVersion().Split('.').Take(3));
            return SemanticVersion.Parse(assemblyVersion + "-dev");
        }

        return currentCliVersion;
    }
}