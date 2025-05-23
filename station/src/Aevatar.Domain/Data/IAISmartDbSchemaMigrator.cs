using System.Threading.Tasks;

namespace Aevatar.Data;

public interface IAevatarDbSchemaMigrator
{
    Task MigrateAsync();
}
