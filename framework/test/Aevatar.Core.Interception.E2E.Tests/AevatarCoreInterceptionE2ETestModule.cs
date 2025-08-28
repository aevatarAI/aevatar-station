using Aevatar.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.Core.Interception.E2E.Tests;

[DependsOn(
    typeof(AevatarTestBaseModule)
)]
public class AevatarCoreInterceptionE2ETestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // This module extends AevatarTestBaseModule which already provides:
        // - ClusterFixture
        // - IClusterClient
        // - IGrainFactory
        // - Orleans cluster infrastructure
        
        // Add any additional services specific to interception E2E tests if needed
    }
}
