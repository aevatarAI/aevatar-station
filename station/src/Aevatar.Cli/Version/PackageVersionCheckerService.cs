using Volo.Abp.DependencyInjection;

namespace Aevatar.Cli.Version;

public class PackageVersionCheckerService : ITransientDependency
{
    public async Task<LatestVersionInfo> GetLatestStableVersionFromGithubAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<LatestVersionInfo> GetLatestVersionOrNullAsync(string packageId, bool includeNightly = false,
        bool includeReleaseCandidates = false)
    {
        throw new NotImplementedException();
    }
}