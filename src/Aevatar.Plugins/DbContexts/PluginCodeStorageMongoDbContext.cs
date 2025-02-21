using Aevatar.Plugins.Entities;
using MongoDB.Driver;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MongoDB;

namespace Aevatar.Plugins.DbContexts;

[ConnectionStringName("Default")]
public class PluginCodeStorageMongoDbContext : AbpMongoDbContext
{
    public IMongoCollection<PluginCodeStorageSnapshotDocument> PluginCodeStorage
    {
        get
        {
            ModelSource = new MongoModelSource();
            ModelSource.GetModel(this);
            return Collection<PluginCodeStorageSnapshotDocument>();
        }
    }

    public PluginCodeStorageMongoDbContext(IServiceProvider serviceProvider)
    {
        LazyServiceProvider = new AbpLazyServiceProvider(serviceProvider);
    }

    protected override void CreateModel(IMongoModelBuilder modelBuilder)
    {
        base.CreateModel(modelBuilder);
        modelBuilder.Entity<PluginCodeStorageSnapshotDocument>(b =>
        {
            b.CollectionName = "StreamStorageAevatar.Plugins.PluginCodeStorageGAgent";
        });
    }
}