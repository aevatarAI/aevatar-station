using Aevatar.Plugins.Entities;
using MongoDB.Driver;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MongoDB;

namespace Aevatar.Plugins.DbContexts;

[ConnectionStringName("Default")]
public class TenantPluginCodeMongoDbContext : AbpMongoDbContext
{
    public IMongoCollection<TenantPluginCodeSnapshotDocument> TenantPluginCode
    {
        get
        {
            ModelSource = new MongoModelSource();
            ModelSource.GetModel(this);
            return Collection<TenantPluginCodeSnapshotDocument>();
        }
    }

    public TenantPluginCodeMongoDbContext(IServiceProvider serviceProvider)
    {
        LazyServiceProvider = new AbpLazyServiceProvider(serviceProvider);
    }

    protected override void CreateModel(IMongoModelBuilder modelBuilder)
    {
        base.CreateModel(modelBuilder);
        modelBuilder.Entity<TenantPluginCodeSnapshotDocument>(b =>
        {
            b.CollectionName = "StreamStorageAevatar.Plugins.TenantPluginCodeGAgent";
        });
    }
}