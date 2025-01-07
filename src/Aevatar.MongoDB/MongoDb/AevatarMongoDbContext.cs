using Aevatar.User;
using MongoDB.Driver;
using Volo.Abp.Data;
using Volo.Abp.MongoDB;

namespace Aevatar.MongoDB;

[ConnectionStringName("Default")]
public class AevatarMongoDbContext : AbpMongoDbContext
{
    /* Add mongo collections here. Example:
     * public IMongoCollection<Question> Questions => Collection<Question>();
     */
    public IMongoCollection<IdentityUserExtension> IdentityUserExtensionInfos { get; private set; }


    protected override void CreateModel(IMongoModelBuilder modelBuilder)
    {
        base.CreateModel(modelBuilder);

        //modelBuilder.Entity<YourEntity>(b =>
        //{
        //    //...
        //});
        modelBuilder.Entity<IdentityUserExtension>(b =>
        {
            b.CollectionName = "IdentityUserExtensions"; 
        });
    }
}
