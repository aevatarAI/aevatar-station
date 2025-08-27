using Volo.Abp.Modularity;

namespace Aevatar;

public abstract class AevatarApplicationTestBase : AevatarTestBase<AevatarApplicationTestModule>
{
}

public abstract class AevatarApplicationTestBase<TStartupModule> : AevatarTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}

