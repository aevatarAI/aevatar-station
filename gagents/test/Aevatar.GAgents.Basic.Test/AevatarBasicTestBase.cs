using Aevatar.GAgents.TestBase;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Basic.Test;

/// <summary>
/// Base class for Aevatar.GAgents.Basic unit tests
/// </summary>
public abstract class AevatarBasicTestBase<TStartupModule> : AevatarGAgentTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
    {
        options.UseAutofac();
    }
}