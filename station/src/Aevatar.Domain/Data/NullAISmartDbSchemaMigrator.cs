using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Data;

/* This is used if database provider does't define
 * IAevatarDbSchemaMigrator implementation.
 */
public class NullAevatarDbSchemaMigrator : IAevatarDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
