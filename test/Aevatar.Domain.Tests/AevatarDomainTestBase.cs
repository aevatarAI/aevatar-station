using Volo.Abp.Modularity;

namespace Aevatar;

/* Inherit from this class for your domain layer tests. */
public abstract class AevatarDomainTestBase<TStartupModule> : AevatarTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
